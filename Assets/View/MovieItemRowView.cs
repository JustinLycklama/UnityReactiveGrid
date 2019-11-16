using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public interface RowStateModifiable {
    void CreateCell(int atIndex, MovieItem forItem, TweenCallback callback);
    void TransposeCell(int atIndex, int toIndex, TweenCallback callback);
}

public class MovieItemRowView : MonoBehaviour, RowStateModifiable {

    public HorizontalLayoutGroup layoutGroup;

    public RectTransform panelTemplate;
    private RectTransform[] panelList;

    public MovieItemCell cellTemplate;
    private Queue<MovieItemCell> reuseQueue = new Queue<MovieItemCell>();
    private MovieItemCell[] activeCells;

    private int columns = 0;

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
                // TODO: also Hide active cell
                reuseQueue.Enqueue(activeCells[i]);                
            }
        }
     
        this.columns = columns;

        panelList = new RectTransform[columns];
        activeCells = new MovieItemCell[columns];

        for(int i = 0; i < columns; i++) {
            RectTransform newTransform = Instantiate(panelTemplate);
            newTransform.transform.SetParent(layoutGroup.transform);

            panelList[i] = newTransform;
        }
    }

    private MovieItemCell DequeueCell() {
        
        if (reuseQueue.Count > 0) {
            return reuseQueue.Dequeue();
        } else {
            MovieItemCell newCell = Instantiate(cellTemplate);

            newCell.transform.SetParent(layoutGroup.transform);
            newCell.rectTransform.sizeDelta = new Vector2(panelList[0].rect.width, panelList[0].rect.height);

            newCell.rectTransform.anchorMin = panelList[0].anchorMin;
            newCell.rectTransform.anchorMax = panelList[0].anchorMax;

            return newCell;
        }
    }

    /*
     * Row State Modifiable Interface
     * */

    public void CreateCell(int atIndex, MovieItem forItem, TweenCallback callback) {
        if (atIndex >= columns) {
            return;
        }

        MovieItemCell newCell = DequeueCell();

        newCell.SetMovieItem(forItem);

        newCell.rectTransform.anchoredPosition = panelList[atIndex].anchoredPosition;
        Vector2 fullSize = newCell.rectTransform.sizeDelta;

        newCell.rectTransform.sizeDelta = Vector2.zero;

        Tween t = newCell.rectTransform.DOSizeDelta(fullSize, 1);

        activeCells[atIndex] = newCell;

        t.OnComplete(callback);
    }

    public void TransposeCell(int atIndex, int toIndex, TweenCallback callback) {

        MovieItemCell cellAtPos = activeCells[atIndex];

        Tween t = cellAtPos.rectTransform.DOAnchorPos(panelList[toIndex].anchoredPosition, 1);

        t.OnComplete(callback);
    }
}
