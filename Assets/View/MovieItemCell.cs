using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MovieItemCell : MonoBehaviour {

    public RectTransform rectTransform;

    public Text text;

    public void SetMovieItem(MovieItem item) {
        text.text = item.title;
    }
}
