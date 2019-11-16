using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// Wishing c# had a typealias so I could strongly type 'Id = int'

public struct MovieItem: IEquatable<MovieItem>, IComparable<MovieItem> {
    public int id { get; private set; }
    public string title { get; private set; }

    public MovieItem(int identifier) {
        id = identifier;
        title = id.ToString();
    }

    public bool Equals(MovieItem other) {
        return id == other.id;
    }

    public int CompareTo(MovieItem other) {
        return id.CompareTo(other.id);
    }
}

public class MovieRequest {

    private List<Action<List<MovieItem>>> endpointList = new List<Action<List<MovieItem>>>();

    public void OpenConnection(Action<List<MovieItem>> connectionEndpoint) {
        endpointList.Add(connectionEndpoint);
    }

    // Imitate results from server
    public void ImmitateConnectionInsert(List<int> movieIds) {

        List<MovieItem> incomingItemList = movieIds.Select(id => ParseObject(id)).ToList();

        foreach(Action<List<MovieItem>> endpoint in endpointList) {
            endpoint(incomingItemList);
        }
    }
    
    // Imitate parse object
    private MovieItem ParseObject(int id) {
        return new MovieItem(id);
    }
}
