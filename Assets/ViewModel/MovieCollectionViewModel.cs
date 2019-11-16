using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public interface MovieUpdateNotifiable {
    void MovieCollectionUpdated(List<MovieItem> collection);
}

public class MovieCollectionViewModel {

    // Delayed singleton
    private static MovieCollectionViewModel backingInstance;
    public static MovieCollectionViewModel sharedInstance {  get {
            if(backingInstance == null) {
                backingInstance = new MovieCollectionViewModel();
            }

            return backingInstance;
        }
    }

    // Attributes
    List<MovieUpdateNotifiable> subscribers;
    Dictionary<int, MovieItem> movieItemDictionary;

    MovieRequest request;

    // Lifecycle
    private MovieCollectionViewModel() {
        subscribers = new List<MovieUpdateNotifiable>();
        movieItemDictionary = new Dictionary<int, MovieItem>();

        request = new MovieRequest();
        request.OpenConnection((List<MovieItem> items) => {
            MergeAndNotify(items);
        });
    }

    // I'm assuming that the server is only returning to us items to be added.
    // An item being removed from the view would only be through the filter.
    private void MergeAndNotify(List<MovieItem> newItems) {

        foreach(MovieItem item in newItems) {
            movieItemDictionary[item.id] = item;
        }

        List<MovieItem> movieCollection = movieItemDictionary.Values.ToList();
        movieCollection.Sort();

        foreach(MovieUpdateNotifiable subscriber in subscribers) {
            subscriber.MovieCollectionUpdated(movieCollection);
        }
    }

    // Subscription
    public void SubscribeToCollectionUpdates(MovieUpdateNotifiable subscriber) {
        subscribers.Add(subscriber);
    }

    public void Unsubscribe(MovieUpdateNotifiable subscriber) {
        subscribers.Remove(subscriber);
    }

    /*
     * Passthrough for testing. Would not exist if connected to server
     * */
     public void SimulateIncomingItems(int[] ids) {
        request.ImmitateConnectionInsert(ids.ToList());
    }
}
