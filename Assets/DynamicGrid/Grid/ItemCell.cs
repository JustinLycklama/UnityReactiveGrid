using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemCell : MonoBehaviour {

    public CanvasGroup canvasGroup;
    public RectTransform rectTransform;
    public Text text;

    public void SetMovieItem(MovieItem item) {
        text.text = item.title;
    }

    public void SetHidden(bool hidden) {
        canvasGroup.alpha = hidden ? 0.1f : 1;
    }
}
