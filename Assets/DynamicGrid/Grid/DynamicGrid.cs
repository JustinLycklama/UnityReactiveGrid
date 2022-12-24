using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using DG.Tweening;

public class DynamicGrid : MonoBehaviour, MovieUpdateNotifiable {

    public VerticalLayoutGroup layoutGroup;

    public ItemRow template;
    private ItemRow[] rowViewList;

    public int numberOfRows;
    public int numberOfColumns;

    private RowAnimationState[] rowAnimationStates;

    private HashSet<int> activeItemsIds = new HashSet<int>();
    private HashSet<int> filteredItemsIds = new HashSet<int>();

    private bool dataUpdated = false;
    private List<MovieItem> newDataCollection = new List<MovieItem>();
    private List<MovieItem> currentAnimationCycleCollection = new List<MovieItem>();

    // Lifecycle

    private void Awake() {
        template.transform.SetParent(null);
    }

    void Start() {
        MovieCollectionViewModel.sharedInstance.SubscribeToCollectionUpdates(this);

        StartCoroutine(WaitForChange());

        SetGridSize(numberOfRows, numberOfColumns);
    }

    private void OnDestroy() {
        MovieCollectionViewModel.sharedInstance.Unsubscribe(this);
    }
    
    public void SetGridSize(int rows, int cols) {

        for(int i = 0; i < numberOfRows; i++) {
            if(rowViewList != null && rowViewList[i] != null) {
                rowViewList[i].transform.SetParent(null);
                Destroy(rowViewList[i].gameObject);
            }
        }

        numberOfRows = rows;
        numberOfColumns = cols;        

        rowAnimationStates = new RowAnimationState[numberOfRows];
        rowViewList = new ItemRow[numberOfRows];

        // Reset all data on grid resize
        activeItemsIds.Clear();
        filteredItemsIds.Clear();

        MovieCollectionViewModel.sharedInstance.ResetData();

        for(int row = 0; row < numberOfRows; row++) {
            rowAnimationStates[row] = new RowAnimationState(numberOfColumns);

            ItemRow rowView = Instantiate(template);
            rowView.transform.SetParent(layoutGroup.transform);

            rowView.SetNumberOfColumns(numberOfColumns);

            rowViewList[row] = rowView;
        }
    }

    // Filter is not an ongoing list, only filter what has been specified to filter
    public void SetFilter(int[] idsToFilter) {
        filteredItemsIds = new HashSet<int>(idsToFilter);
        dataUpdated = true;
    }

    private void UpdateRowStateGivenCollection(List<MovieItem> collection) {
        for(int row = 0; row < numberOfRows; row++) {
            List<MovieItem> rowItems = collection.Skip(row * numberOfColumns).Take(numberOfColumns).ToList();

            rowAnimationStates[row].SetRowItems(rowItems, activeItemsIds, filteredItemsIds);
        }
    }

    private void PrintStateChange() {
        print("State Change");
        print("");

        int row = 0;
        foreach(RowAnimationState state in rowAnimationStates) {
            print("Row " + row + ":");
            state.Print();
            row++;
        }

        print("");
    }
  
    /*
     * Movie Update Notifiable Interface
     * */

    public void MovieCollectionUpdated(List<MovieItem> collection) {
        newDataCollection = collection;
        dataUpdated = true;
    }
   
    /*
     * Animation States
     * */

    IEnumerator WaitForChange() {

        yield return new WaitUntil(() => { return dataUpdated; });
        dataUpdated = false;

        currentAnimationCycleCollection = newDataCollection.Where(item => !filteredItemsIds.Contains(item.id)).ToList();

        // Update State
        UpdateRowStateGivenCollection(currentAnimationCycleCollection);
        //PrintStateChange();

        // Perform Animations
        StartCoroutine(PerformAnimateUpdate(new List<CellPhase> { CellPhase.Delete, CellPhase.Transpose, CellPhase.Create }));
    }

    IEnumerator PerformAnimateUpdate(List<CellPhase> phases) {

        if (phases.Count == 0) {
            CompleteAnimateUpdate();
            yield break; 
        }

        CellPhase phase = phases[0];
        phases.RemoveAt(0);

        int animations = 0;
        for(int row = 0; row < numberOfRows; row++) {

            RowAnimationState state = rowAnimationStates[row];
            ItemRow rowView = rowViewList[row];

            animations += state.EnactModificationsOnObject(rowView, phase, () => { animations--; });
        }

        yield return new WaitUntil(() => animations == 0);

        StartCoroutine(PerformAnimateUpdate(phases));
    }

    private void CompleteAnimateUpdate() {

        // Cleanup and reset
        activeItemsIds = new HashSet<int>(currentAnimationCycleCollection.Select(x => x.id));

        for(int row = 0; row < numberOfRows; row++) {
            rowViewList[row].Consolidate();
        }

        StartCoroutine(WaitForChange());
    }
}

/* 
 * State Consolidation 
 *
 * This is where we keep track of what values are currently in a row, and how we determine what will happen when a row is given a new set of values.
 * Actions that need to take place are split into three phases, delete, transpose, and add. This way we can perform each animation phase after the previous has completed
 * 
 * */

public enum CellPhase {
    Delete,      
    Transpose,     
    Create       
}

struct CellPhaseAction {
    public int moveTo { get; private set; }
    public MovieItem? addItem { get; private set; }

    public CellPhaseAction(int moveTo = 0, MovieItem? addItem = null) : this() {
        this.moveTo = moveTo;
        this.addItem = addItem;
    }

    public CellPhaseAction(MovieItem? addItem) : this(0, addItem) {}
}

public class RowAnimationState {

    private int columns;

    private MovieItem?[] cellItems;
    private Dictionary<CellPhase, CellPhaseAction>[] cellPhaseActionsList;

    private List<CellPhaseAction> leftSideboardTransposes;
    private List<CellPhaseAction> rightSideboardTransposes;

    private int leftExitIndex = -1;
    private int rightExitIndex = 0;

    public RowAnimationState(int columns) {
        this.columns = columns;
        
        cellItems = new MovieItem?[columns];
        cellPhaseActionsList = new Dictionary<CellPhase, CellPhaseAction>[columns];

        leftSideboardTransposes = new List<CellPhaseAction>();
        rightSideboardTransposes = new List<CellPhaseAction>();

        for(int i = 0; i < columns; i++) {
            cellPhaseActionsList[i] = new Dictionary<CellPhase, CellPhaseAction>();
        }
    }

    public void SetRowItems(List<MovieItem> newRowItems, HashSet<int> activeItemIds, HashSet<int> filteredItemIds) {

        if(newRowItems.Count > columns) {
            MonoBehaviour.print("** Tried to insert too many items into a row **");
            return;
        }

        UpdateCellPhaseMapping(newRowItems, activeItemIds, filteredItemIds);

        for(int i = 0; i < columns; i++) {
            if (i < newRowItems.Count) {
                cellItems[i] = newRowItems[i];
            } else {
                cellItems[i] = null;
            }
        }
    }

    private void UpdateCellPhaseMapping(List<MovieItem> newRowItems, HashSet<int> activeItemIds, HashSet<int> filteredItemIds) {
        // These Ids are dealt with in the initial pass, as we delete or move cells that already existed in this row
        HashSet<int> dealtWithIds = new HashSet<int>();

        // Reset state
        for(int i = 0; i < columns; i++) {
            cellPhaseActionsList[i].Clear();
        }

        leftSideboardTransposes.Clear();
        rightSideboardTransposes.Clear();

        leftExitIndex = -1;
        rightExitIndex = 0;

        // Do first pass, delete existing cells or move them to appropriate place
        for(int i = 0; i < columns; i++) {

            if(cellItems[i] == null) {
                continue;
            }

            MovieItem oldCellItem = cellItems[i].Value;
            MovieItem? newCellItem = null;

            if(newRowItems.Count > i) {
                newCellItem = newRowItems[i];
            }

            dealtWithIds.Add(oldCellItem.id);

            if(filteredItemIds.Contains(oldCellItem.id)) {
                // This item is getting removed, queue a deletion
                cellPhaseActionsList[i][CellPhase.Delete] = new CellPhaseAction();
            } else if(newCellItem.HasValue && newCellItem.Value.id == oldCellItem.id) {
                // No action will come of this cell
            } else {

                // If this cell is not deleted and not at a standstill, lets see where it is moving to...
                List<int> newRowIds = newRowItems.Select(x => x.id).ToList();
                int moveToIndex = newRowIds.IndexOf(oldCellItem.id);

                // It is moving to a concrete index
                if(moveToIndex >= 0) {
                    cellPhaseActionsList[i][CellPhase.Transpose] = new CellPhaseAction(moveToIndex);
                    continue;
                }

                // If it is not moving somewhere in our row, it is moving to another row
                if(newRowItems.Count == 0 || oldCellItem.CompareTo(newRowItems[0]) < 0) {
                    cellPhaseActionsList[i][CellPhase.Transpose] = new CellPhaseAction(leftExitIndex);
                    leftExitIndex--;
                } else {
                    cellPhaseActionsList[i][CellPhase.Transpose] = new CellPhaseAction(columns + rightExitIndex);
                    rightExitIndex++;
                }
            }
        }

        // Second pass, transition new items that did not exist from either edge or create new
        for(int i = 0; i < columns; i++) {
            if(i >= newRowItems.Count || dealtWithIds.Contains(newRowItems[i].id)) {
                continue;
            }

            MovieItem newItem = newRowItems[i];            

            if(!activeItemIds.Contains(newItem.id)) {
                cellPhaseActionsList[i][CellPhase.Create] = new CellPhaseAction(newItem);
                continue;
            }

            // At this point the new item has not been handled, and previously existed elsewhere. We need to transition from an edge
            List<int> newRowIds = newRowItems.Select(x => x.id).ToList();
            int moveToIndex = newRowIds.IndexOf(newItem.id);

            if(cellItems[0].HasValue == false || newItem.CompareTo(cellItems[0].Value) < 0) {
                leftSideboardTransposes.Add(new CellPhaseAction(moveToIndex, newItem));
            } else {
                rightSideboardTransposes.Add(new CellPhaseAction(moveToIndex, newItem));
            }
        }

        // The last item added on the left sideboard should be on the near edge of the screen
        leftSideboardTransposes.Reverse();
    }    

    // Returns the number of animations kicked off by these modifications
    public int EnactModificationsOnObject(RowStateModifiable modifiable, CellPhase forPhase, TweenCallback tweenCallback) {

        int animationCount = 0;

        for(int i = 0; i < columns; i++) {
            Dictionary<CellPhase, CellPhaseAction> actions = cellPhaseActionsList[i];

            switch(forPhase) {
                case CellPhase.Delete:
                    if(actions.ContainsKey(CellPhase.Delete)) {
                        modifiable.DeleteCell(i, tweenCallback);
                        animationCount++;
                    }
                    break;
                case CellPhase.Transpose:
                    if(actions.ContainsKey(CellPhase.Transpose)) {
                        modifiable.TransposeCell(i, actions[CellPhase.Transpose].moveTo, tweenCallback, null);
                        animationCount++;
                    }
                    break;
                case CellPhase.Create:
                    if(actions.ContainsKey(CellPhase.Create)) {
                        modifiable.CreateCell(i, actions[CellPhase.Create].addItem.Value, tweenCallback);
                        animationCount++;
                    }
                    break;
            }                    
        }

        if (forPhase == CellPhase.Transpose) {
            // Sideboard mapping: 0 = -1, 1 = -2, 2 = -3, ...
            for(int i = 0; i < leftSideboardTransposes.Count; i++) {
                CellPhaseAction leftPhaseAction = leftSideboardTransposes[i];

                modifiable.TransposeCell((-1 - i), leftPhaseAction.moveTo, tweenCallback, leftPhaseAction.addItem);
                animationCount++;
            }

            // Assuming columns == 3, Sideboard mapping: 0 = 3, 1 = 4, 2 = 5, ...
            for(int i = 0; i < rightSideboardTransposes.Count; i++) {
                CellPhaseAction rightPhaseAction = rightSideboardTransposes[i];

                modifiable.TransposeCell(i + columns, rightPhaseAction.moveTo, tweenCallback, rightPhaseAction.addItem);
                animationCount++;
            }
        }        

        return animationCount;
    }

    public void Print() {
        for(int i = 0; i < columns; i++) {
            string cellDetails = "column " + i + " will: ";

            if (cellPhaseActionsList[i].Count == 0) {
                cellDetails += "Do nothing.";
            }

            if (cellPhaseActionsList[i].ContainsKey(CellPhase.Delete)) {
                cellDetails += "Delete Item, "; 
            }

            if(cellPhaseActionsList[i].ContainsKey(CellPhase.Transpose)) {
                cellDetails += "Move Item to " + cellPhaseActionsList[i][CellPhase.Transpose].moveTo + ", ";
            }

            if(cellPhaseActionsList[i].ContainsKey(CellPhase.Create)) {
                cellDetails += "Create Cell " + cellPhaseActionsList[i][CellPhase.Create].addItem.Value.id + ".";
            }

            MonoBehaviour.print(cellDetails); 
        }         

        for(int i = 0; i < leftSideboardTransposes.Count; i++) {
            CellPhaseAction leftPhaseAction = leftSideboardTransposes[i];

            string cellDetails = "leftSideBoard #" + i + " will: ";
            cellDetails += "Move new cell " + leftPhaseAction.addItem.Value.id + " to " + leftPhaseAction.moveTo + ".";
            MonoBehaviour.print(cellDetails);
        }

        for(int i = 0; i < rightSideboardTransposes.Count; i++) {
            CellPhaseAction rightPhaseAction = rightSideboardTransposes[i];

            string cellDetails = "leftSideBoard #" + i + " will: ";
            cellDetails += "Move new cell " + rightPhaseAction.addItem.Value.id +  " to " + rightPhaseAction.moveTo + ".";
        }
    }
}