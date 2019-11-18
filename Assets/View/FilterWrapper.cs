using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

/*
 * Since filtering via text doesn't exist in this example, lets use this class for our test cases
 * */
public class FilterWrapper : MonoBehaviour {

    public MovieDisplayGrid displayGrid;

    // Functions
    public Button runTestButton;
    public Button randomizeButton;

    // Grid Size
    public InputField rows;
    public InputField columns;

    public Button submitGridSize;

    // Data Input
    public InputField newItemField;
    public Button addItemButton;

    private List<int> newItemsList = new List<int>();
    public Text incomingItemsText;
    public Button sendItems;

    // Filter
    public InputField newFilterField;
    public Button addFilterButton;

    private List<int> filtersList = new List<int>();
    public Text filtersText;
    public Button clearFilter;

    private void Start() {

        // Functions
        runTestButton.onClick.AddListener(() => {
            QueueTests();
        });

        randomizeButton.onClick.AddListener(() => {
            int numberOfAdds = Mathf.RoundToInt(displayGrid.numberOfColumns * displayGrid.numberOfRows / 2);

            List<int> intList = new List<int>();
            System.Random r = new System.Random();

            for(int i = 0; i < numberOfAdds; i++) {
                intList.Add(r.Next(1, 100));
            }

            MovieCollectionViewModel.sharedInstance.SimulateIncomingItems(intList.ToArray());
        });

        // Grid Size
        submitGridSize.onClick.AddListener(() => {
            if(this.rows.text.Length == 0 || this.columns.text.Length == 0) { return; }

            int rows = int.Parse(this.rows.text);
            int columns = int.Parse(this.columns.text);

            displayGrid.SetGridSize(rows, columns);        
        });

        // New Items
        addItemButton.onClick.AddListener(() => {
            if(newItemField.text.Length == 0) { return; }

            int item = int.Parse(this.newItemField.text);
            newItemField.text = "";

            newItemsList.Add(item);
            incomingItemsText.text = newItemsList.Select(x => x.ToString()).Aggregate((x, y) => x + ", " + y + " ");
        });

        sendItems.onClick.AddListener(() => {
            MovieCollectionViewModel.sharedInstance.SimulateIncomingItems(newItemsList.ToArray());

            newItemsList.Clear();
            incomingItemsText.text = "";
        });

        // Filter Items
        addFilterButton.onClick.AddListener(() => {
            if (newFilterField.text.Length == 0) { return; }

            int item = int.Parse(this.newFilterField.text);
            newFilterField.text = "";

            filtersList.Add(item);
            filtersText.text = filtersList.Select(x => x.ToString()).Aggregate((x, y) => x + ", " + y + " ");
            displayGrid.SetFilter(filtersList.ToArray());
        });

        clearFilter.onClick.AddListener(() => {
            filtersList.Clear();
            filtersText.text = "";

            displayGrid.SetFilter(filtersList.ToArray());
        });
    }
   
    /*
     * Test Cases
     * */

    enum EditType {
        Add,
        Filter
    }

    IEnumerator RunTests(List<Action<Action>> tests) {
        foreach(Action<Action> test in tests) {
            bool performing = true;
            test(() => performing = false );
            yield return new WaitUntil(() => !performing);
        }
    }

    private void QueueTests() {

        List<Action<Action>> functionList = new List<Action<Action>>();

        functionList.Add(SimpleAdditon);
        functionList.Add(SimpleAdditonTwo);

        functionList.Add(SimpleTranspose);
        functionList.Add(SequentialTranspose);
        functionList.Add(TransposeOutOfRow);
        functionList.Add(TransposeToNewRow);

        functionList.Add(SimpleFilter);
        functionList.Add(ClearFilter);
        functionList.Add(AdvancedClearFilter);

        functionList.Add(LargeTest);

        StartCoroutine(RunTests(functionList));
    }

    /*
     * Additions
     * */

    private void SimpleAdditon(Action callback) {

        displayGrid.SetGridSize(1, 3);

        List<Tuple<EditType, int[]>> data = new List<Tuple<EditType, int[]>>();

        data.Add(new Tuple<EditType, int[]>(
            EditType.Add, 
            new[] { 1, 2 }) );

        data.Add(new Tuple<EditType, int[]>(
            EditType.Add,
            new[] { 3, 4 }));        

        StartCoroutine(RunTest(data, callback, 0.25f));
    }

    private void SimpleAdditonTwo(Action callback) {
        displayGrid.SetGridSize(2, 3);

        List<Tuple<EditType, int[]>> data = new List<Tuple<EditType, int[]>>();

        data.Add(new Tuple<EditType, int[]>(   
            EditType.Add,    
            new[] { 1, 2 }));

        data.Add(new Tuple<EditType, int[]>(
            EditType.Add,
            new[] { 3, 4 }));

        StartCoroutine(RunTest(data, callback));
    }

    /*
     * Transposes
     * */

    private void SimpleTranspose(Action callback) {

        displayGrid.SetGridSize(1, 3);

        List<Tuple<EditType, int[]>> data = new List<Tuple<EditType, int[]>>();

        data.Add(new Tuple<EditType, int[]>(
            EditType.Add,
            new[] { 1, 3 }));

        data.Add(new Tuple<EditType, int[]>(
            EditType.Add,
            new[] { 2 }));

        StartCoroutine(RunTest(data, callback));
    }

    private void SequentialTranspose(Action callback) {

        displayGrid.SetGridSize(1, 3);

        List<Tuple<EditType, int[]>> data = new List<Tuple<EditType, int[]>>();

        data.Add(new Tuple<EditType, int[]>(
            EditType.Add,
            new[] { 9 }));

        data.Add(new Tuple<EditType, int[]>(
            EditType.Add,
            new[] { 8 }));

        data.Add(new Tuple<EditType, int[]>(
            EditType.Add,
            new[] { 7 }));

        StartCoroutine(RunTest(data, callback));
    }

    private void TransposeOutOfRow(Action callback) {
        displayGrid.SetGridSize(1, 3);

        List<Tuple<EditType, int[]>> data = new List<Tuple<EditType, int[]>>();

        data.Add(new Tuple<EditType, int[]>(
            EditType.Add,
            new[] { 1, 2, 4 }));

        data.Add(new Tuple<EditType, int[]>(
            EditType.Add,
            new[] { 3 }));

        StartCoroutine(RunTest(data, callback));
    }

    private void TransposeToNewRow(Action callback) {
        displayGrid.SetGridSize(2, 3);

        List<Tuple<EditType, int[]>> data = new List<Tuple<EditType, int[]>>();

        data.Add(new Tuple<EditType, int[]>(
            EditType.Add,
            new[] { 1, 2, 4 }));

        data.Add(new Tuple<EditType, int[]>(
            EditType.Add,
            new[] { 3 }));

        StartCoroutine(RunTest(data, callback));
    }

    /*
     * Deletions
     * */

    private void SimpleFilter(Action callback) {
        displayGrid.SetGridSize(1, 3);

        List<Tuple<EditType, int[]>> data = new List<Tuple<EditType, int[]>>();

        data.Add(new Tuple<EditType, int[]>(
            EditType.Add,
            new[] { 1, 3 }));

        data.Add(new Tuple<EditType, int[]>(
            EditType.Add,
            new[] { 2 }));

        data.Add(new Tuple<EditType, int[]>(
            EditType.Filter,
            new[] { 1 }));

        StartCoroutine(RunTest(data, callback));
    }

    private void ClearFilter(Action callback) {
        displayGrid.SetGridSize(1, 3);

        List<Tuple<EditType, int[]>> data = new List<Tuple<EditType, int[]>>();

        data.Add(new Tuple<EditType, int[]>(
            EditType.Add,
            new[] { 1, 3 }));

        data.Add(new Tuple<EditType, int[]>(
            EditType.Add,
            new[] { 2 }));

        data.Add(new Tuple<EditType, int[]>(
            EditType.Filter,
            new[] { 1 }));

        data.Add(new Tuple<EditType, int[]>(
            EditType.Filter,
            new int[0]));

        StartCoroutine(RunTest(data, callback, 1.2f));
    }

    private void AdvancedClearFilter(Action callback) {
        displayGrid.SetGridSize(2, 3);

        List<Tuple<EditType, int[]>> data = new List<Tuple<EditType, int[]>>();

        data.Add(new Tuple<EditType, int[]>(
            EditType.Add,
            new[] { 3, 5 , 7, 9 }));

        data.Add(new Tuple<EditType, int[]>(
            EditType.Add,
            new[] { 2 }));

        data.Add(new Tuple<EditType, int[]>(
            EditType.Filter,
            new[] { 5 }));

        data.Add(new Tuple<EditType, int[]>(
            EditType.Add,
            new[] { 6 }));

        data.Add(new Tuple<EditType, int[]>(
            EditType.Filter,
            new int[0]));

        data.Add(new Tuple<EditType, int[]>(
            EditType.Filter,
            new[] { 5 }));

        data.Add(new Tuple<EditType, int[]>(
            EditType.Filter,
            new int[0]));

        StartCoroutine(RunTest(data, callback, 1.5f));
    }

    private void LargeTest(Action callback) {
        displayGrid.SetGridSize(5, 5);

        List<Tuple<EditType, int[]>> data = new List<Tuple<EditType, int[]>>();

        data.Add(new Tuple<EditType, int[]>(
            EditType.Add,
            new[] { 32, 68, 47, 19, 22, 35, 86, 93, 39, 44, 60, 10 }));

        data.Add(new Tuple<EditType, int[]>(
            EditType.Add,
            new[] { 12, 46, 80, 99 }));

        data.Add(new Tuple<EditType, int[]>(
            EditType.Filter,
            new[] { 47, 35, 93 }));

        data.Add(new Tuple<EditType, int[]>(
            EditType.Add,
            new[] { 61, 27, 5 }));

        StartCoroutine(RunTest(data, callback));
    }

    IEnumerator RunTest(List<Tuple<EditType, int[]>> simulatedDataList, Action callback, float delay = 0.65f) {

        foreach(Tuple<EditType, int[]> data in simulatedDataList) {
            switch(data.Item1) {
                case EditType.Add:
                    MovieCollectionViewModel.sharedInstance.SimulateIncomingItems(data.Item2);
                    break;
                case EditType.Filter:
                    displayGrid.SetFilter(data.Item2);
                    break;
            }

            yield return new WaitForSeconds(delay);
        }

        yield return new WaitForSeconds(2f);

        callback();
    }
}
