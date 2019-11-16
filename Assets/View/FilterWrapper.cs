using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/*
 * Since filtering via text doesn't exist in this example, lets use this class for our test cases
 * */
public class FilterWrapper : MonoBehaviour {

    public MovieDisplayGrid displayGrid;

    private void Start() {
        RunTests();
    }

    //private void PrintGridState() {
    //    RowAnimationState[] animationStates = displayGrid.GetRowStates();

    //    print("State Change");
    //    print("");

    //    int row = 0;
    //    foreach(RowAnimationState state in animationStates) {
    //        print("Row " + row + ":");
    //        state.Print();
    //        row++;
    //    }

    //    print("");
    //}

    /*
     * Test Cases
     * */

    private void RunTests() {
        //SimpleAdditon();
        //SimpleAdditonTwo();

        SimpleTranspose();
    }

    private void SimpleAdditon() {

        displayGrid.SetGridSize(1, 3);

        List<int[]> data = new List<int[]>();

        data.Add(new[] { 1, 2 });
        data.Add(new[] { 3, 4 });
        //data.Add(new[] { 0, 5 });

        StartCoroutine(RunTest(data, 0.25f));
    }

    private void SimpleAdditonTwo() {
        displayGrid.SetGridSize(2, 3);

        List<int[]> data = new List<int[]>();

        data.Add(new[] { 1, 2 });
        data.Add(new[] { 3, 4 });
        //data.Add(new[] { 0, 5 });

        StartCoroutine(RunTest(data));
    }

    private void SimpleTranspose() {

        displayGrid.SetGridSize(1, 3);

        List<int[]> data = new List<int[]>();

        data.Add(new[] { 1, 3 });
        data.Add(new[] { 2 });

        StartCoroutine(RunTest(data));
    }

    IEnumerator RunTest(List<int[]> simulatedDataList, float delay = 1.2f) {

        foreach(int[] data in simulatedDataList) {
            MovieCollectionViewModel.sharedInstance.SimulateIncomingItems(data);

            yield return new WaitForSeconds(delay);
        }
    }
}
