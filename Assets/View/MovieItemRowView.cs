using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public interface RowStateModifiable {
    void CreateCell(int atIndex, MovieItem forItem, TweenCallback callback);
    void TransposeCell(int atIndex, int toIndex, TweenCallback callback, MovieItem? withNewItem);
    void DeleteCell(int atIndex, TweenCallback callback);
    void Consolidate();
}

public class MovieItemRowView : MonoBehaviour, RowStateModifiable {

    public RectTransform rectTransform;
    public HorizontalLayoutGroup layoutGroup;

    public RectTransform panelTemplate;
    private RectTransform[] panelList;

    public MovieItemCell cellTemplate;
    private Queue<MovieItemCell> reuseQueue = new Queue<MovieItemCell>();

    private MovieItemCell[] activeCells;
    private MovieItemCell[] nextTransitionCells;

    private int columns = 0;

    private const float animationDuration = 0.65f;

    private void Awake() {
        cellTemplate.transform.SetParent(null);
        panelTemplate.transform.SetParent(null);
    }

    void Start() {

        DOTween.Init();

        //RectTransform rect = anyCell.GetComponent<RectTransform>();


        //rect.position = itemPanelList[0].position;
        //rect.sizeDelta = itemPanelList[0].sizeDelta;

        //anyCell.rect.Set(0, 0, 100, 100);

        Invoke("GetRectSet", 0.01f); ///on your start function to delay it a bit.    

    }

    void GetRectSet() {
        //Set the Layout Min Value equivaline to RectTransform
        //anyCell.sizeDelta = new Vector2(itemPanelList[0].rect.width, itemPanelList[0].rect.height);

        //anyCell.anchorMin = itemPanelList[0].anchorMin;
        //anyCell.anchorMax = itemPanelList[0].anchorMax;

        //anyCell.anchoredPosition = itemPanelList[0].anchoredPosition;

        //anyCell.DOAnchorPos(itemPanelList[2].anchoredPosition, 1);



    }

    public void SetNumberOfColumns(int columns) {        
        if (columns <= 0) {
            columns = 1;
        }

        for(int i = 0; i < columns; i++) {
            if (panelList != null && panelList[i] != null) {
                panelList[i].SetParent(null);
                Destroy(panelList[i].gameObject);
            }

            if (activeCells != null && activeCells[i] != null) {
                // Hide cell off screen
                activeCells[i].rectTransform.anchoredPosition = CellPositionForIndex(-1);
                reuseQueue.Enqueue(activeCells[i]);                
            }
        }
     
        this.columns = columns;

        panelList = new RectTransform[columns];
        activeCells = new MovieItemCell[columns];
        nextTransitionCells = new MovieItemCell[columns];

        for(int i = 0; i < columns; i++) {
            RectTransform newTransform = Instantiate(panelTemplate);
            newTransform.transform.SetParent(layoutGroup.transform);

            panelList[i] = newTransform;
        }
    }

    private MovieItemCell DequeueCell(MovieItem item) {

        MovieItemCell cell;

        if (reuseQueue.Count > 0) {
            cell = reuseQueue.Dequeue();
        } else {
            MovieItemCell newCell = Instantiate(cellTemplate);

            newCell.transform.SetParent(layoutGroup.transform);
            newCell.rectTransform.sizeDelta = new Vector2(panelList[0].rect.width, panelList[0].rect.height);

            newCell.rectTransform.anchorMin = panelList[0].anchorMin;
            newCell.rectTransform.anchorMax = panelList[0].anchorMax;

            cell = newCell;
        }

        cell.SetHidden(false);
        cell.SetMovieItem(item);

        return cell;
    }

    private void EnqueueCell(MovieItemCell cell) {
        cell.rectTransform.anchoredPosition = CellPositionForIndex(-1);
        cell.SetHidden(true);

        reuseQueue.Enqueue(cell);
    }

    private Vector2 CellPositionForIndex(int index) {

        if (index < -columns || index > (columns * 2 - 1)) {
            return Vector2.zero;
        }

        Vector2 viewWidth = new Vector2(rectTransform.rect.width, 0);

        if (index < 0) {
            return panelList[index * -1].anchoredPosition - viewWidth;
        }

        else if (index >= 0 && index < columns) {
            return panelList[index].anchoredPosition;
        }

        else { // if (index >= columns)
            return panelList[index - columns].anchoredPosition + viewWidth;
        }
    }

    /*
     * Row State Modifiable Interface
     * */

    public void CreateCell(int atIndex, MovieItem forItem, TweenCallback callback) {
        if (atIndex >= columns) {
            return;
        }

        MovieItemCell newCell = DequeueCell(forItem);

        newCell.rectTransform.anchoredPosition = CellPositionForIndex(atIndex);
        Vector2 fullSize = newCell.rectTransform.sizeDelta;

        newCell.rectTransform.sizeDelta = Vector2.zero;

        Tween t = newCell.rectTransform.DOSizeDelta(fullSize, animationDuration);

        nextTransitionCells[atIndex] = newCell;

        t.OnComplete(callback);
    }

    /*
     * atIndex and toIndex can fall outside of our number of columns to accomodate the left and right sideboard
     * */
    public void TransposeCell(int atIndex, int toIndex, TweenCallback callback, MovieItem? withNewItem = null) {

        MovieItemCell cellAtPos;
        
        if (atIndex >= 0 && atIndex < columns) {
            cellAtPos = activeCells[atIndex];
        } else {
            if (!withNewItem.HasValue) {
                return;
            }

            cellAtPos = DequeueCell(withNewItem.Value);
            cellAtPos.rectTransform.anchoredPosition = CellPositionForIndex(atIndex);
        }

        Tween t = cellAtPos.rectTransform.DOAnchorPos(CellPositionForIndex(toIndex), animationDuration);

        // Don't bother saving the new state if it is transitioning off screen
        if (toIndex >= 0 && toIndex < columns) {
            nextTransitionCells[toIndex] = cellAtPos;
        } 

        t.OnComplete(() => {
            // If the cell is offscreen, prepare for reuse
            if(toIndex < 0 || toIndex >= columns) {
                EnqueueCell(cellAtPos);
            }

            callback();
        });
    }

    public void DeleteCell(int atIndex, TweenCallback callback) {
        if(atIndex >= columns) {
            return;
        }

        MovieItemCell oldCell = activeCells[atIndex];

        Vector2 fullSize = oldCell.rectTransform.sizeDelta;

        Tween t = oldCell.rectTransform.DOSizeDelta(Vector2.zero, animationDuration);

        nextTransitionCells[atIndex] = null;

        t.OnComplete(() => {
            // Hide cell and return to regular size
            oldCell.rectTransform.sizeDelta = fullSize;
            EnqueueCell(oldCell);

            callback();
        });
    }

    public void Consolidate() {


        string oldState = "";
        string newState = "";

        for(int i = 0; i < columns; i++) {

            if(activeCells[i] == null) {
                oldState += "null, ";
            } else {
                oldState += activeCells[i].text.text + ", ";
            }

            if(nextTransitionCells[i] == null) {
                newState += "null, ";
            } else {
                newState += nextTransitionCells[i].text.text + ", ";
            }
        }

        print("Active Cells Moves From States: ");
        print(oldState);
        print(newState);

        Array.Copy(nextTransitionCells, activeCells, columns);
    }
}
