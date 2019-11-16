using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;

public class MovieDisplayGrid : MonoBehaviour, MovieUpdateNotifiable {

    public VerticalLayoutGroup layoutGroup;

    public MovieItemRowView template;
    private MovieItemRowView[] rowViewList;

    private int numberOfRows = 0;
    private int numberOfColumns = 0;

    private RowAnimationState[] rowAnimationStates;

    private HashSet<int> activeItemsIds = new HashSet<int>();
    private HashSet<int> filteredItemsIds = new HashSet<int>();

    private bool dataUpdated = false;
    private List<MovieItem> newDataCollection = null;

    // Lifecycle

    private void Awake() {
        template.transform.SetParent(null);
    }

    void Start() {
        MovieCollectionViewModel.sharedInstance.SubscribeToCollectionUpdates(this);

        StartCoroutine(WaitForChange());
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
        rowViewList = new MovieItemRowView[numberOfRows];

        // Reset all data on grid resize
        activeItemsIds.Clear();
        filteredItemsIds.Clear();

        for(int row = 0; row < numberOfRows; row++) {
            rowAnimationStates[row] = new RowAnimationState(numberOfColumns);

            MovieItemRowView rowView = Instantiate(template);
            rowView.transform.SetParent(layoutGroup.transform);

            rowView.SetNumberOfColumns(numberOfColumns);

            rowViewList[row] = rowView;
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
     * Data Management
     * */

    private void UpdateRowStateGivenCollection(List<MovieItem> collection) {
        for(int row = 0; row < numberOfRows; row++) {
            List<MovieItem> rowItems = collection.Skip(row * numberOfColumns).Take(numberOfColumns).ToList();

            rowAnimationStates[row].SetRowItems(rowItems, activeItemsIds, filteredItemsIds);
        }
    }

    public void PerformAllAnimationsForNewState() {
        for(int row = 0; row < numberOfRows; row++) {

            RowAnimationState state = rowAnimationStates[row];            
            MovieItemRowView rowView = rowViewList[row];

            state.EnactModificationsOnObject(rowView);            
        }
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
        AnimateUpdate();
    }

    private void AnimateUpdate() {
        UpdateRowStateGivenCollection(newDataCollection);
        PrintStateChange();

        PerformAllAnimationsForNewState();

        activeItemsIds = new HashSet<int>(newDataCollection.Select(x => x.id));

        StartCoroutine(WaitForChange());
    }
}

enum CellPhase {
    Delete,      
    Transpose,     
    Create       
}

struct CellPhaseAction {
    public int moveTo { get; private set; }
    public MovieItem? addItem { get; private set; }

    public CellPhaseAction(int moveTo) : this() {
        this.moveTo = moveTo;
    }

    public CellPhaseAction(MovieItem? addItem) : this() {
        this.addItem = addItem;
    }
}

public class RowAnimationState {

    private int columns;

    private MovieItem?[] cellItems;
    private Dictionary<CellPhase, CellPhaseAction>[] cellPhaseActionsList;

    private int leftExitIndex = -1;
    private int rightExitIndex = 0;

    public RowAnimationState(int columns) {
        this.columns = columns;
        
        cellItems = new MovieItem?[columns];
        cellPhaseActionsList = new Dictionary<CellPhase, CellPhaseAction>[columns];

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


        }
    }

    public void EnactModificationsOnObject(RowStateModifiable modifiable) {

        for(int i = 0; i < columns; i++) {
            Dictionary<CellPhase, CellPhaseAction> actions = cellPhaseActionsList[i];

            if (actions.ContainsKey(CellPhase.Transpose)) {
                modifiable.TransposeCell(i, actions[CellPhase.Transpose].moveTo, () => { });
            }

            if (actions.ContainsKey(CellPhase.Create)) {
                modifiable.CreateCell(i, actions[CellPhase.Create].addItem.Value, () => { });
            }
        }
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
    }


}