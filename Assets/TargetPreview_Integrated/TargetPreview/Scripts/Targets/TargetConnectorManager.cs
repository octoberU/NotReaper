using System;
using System.Collections.Generic;
using TargetPreview.Math;
using UnityEngine;

namespace TargetPreview.Scripts.Targets
{
    public class TargetConnectorManager : MonoBehaviour, IReceiveTimeUpdates
    {
        HashSet<(TargetReference firstCue, TargetReference secondCue)> activeConnections = new();
        [SerializeField] TargetConnector connectorPrefab;
        List<TargetConnector> activeConnectors = new();
        List<TargetConnector> connectorPool = new();
        const int poolSize = 20;

        void Awake()
        {
            InitializePool();
            TimeController.AddListener(this);
        }

        void OnDestroy() =>
            TimeController.RemoveListener(this);

        void InitializePool()
        {
            for (int i = 0; i < poolSize; i++)
            {
                AddtoPool();
            }
        }

        void AddtoPool()
        {
            var connector = Instantiate(connectorPrefab, transform);
            connector.gameObject.SetActive(false);
            connectorPool.Add(connector);
        }

        void ActivateConnection((TargetReference, TargetReference) references)
        {
            if(connectorPool.Count == 0)
            {
                AddtoPool();
            }
            var connector = connectorPool[0];
            connectorPool.RemoveAt(0);
            connector.Initialize(references.Item1.target, references.Item2.target);
            connector.UpdateConnector();
            connector.gameObject.SetActive(true);
            activeConnectors.Add(connector);
        }
        
        void DeactivateConnection(TargetReference firstCue, TargetReference secondCue)
        {
            TargetConnector foundConnector = null;
            foreach (var activeConnector in activeConnectors)
            {
                if (activeConnector.firstTarget == firstCue.target)
                {
                    foundConnector = activeConnector;
                    break;
                }
            }
            foundConnector.gameObject.SetActive(false);
            connectorPool.Add(foundConnector);
            activeConnectors.Remove(foundConnector);
        }

        public bool TryAddConnection(TargetReference firstCue, TargetReference secondCue)
        {
            var connection = (firstCue, secondCue);
            var newConnection = !activeConnections.Contains(connection) && !activeConnections.Contains((secondCue, firstCue));
            
            if(newConnection)
            {
                activeConnections.Add(connection);
                ActivateConnection(connection);
            }
            
            return newConnection;
        }
        
        public void RemoveConnection(TargetReference firstCue, TargetReference secondCue)
        {
            bool foundConnection = activeConnections.Remove((firstCue, secondCue)) || activeConnections.Remove((secondCue, firstCue));

            if(foundConnection)
            {
                DeactivateConnection(firstCue, secondCue);
            }
        }

        // void OnDrawGizmos()
        // {
        //     foreach (var connection in activeConnections)
        //     {
        //         var firstCue = connection.firstCue;
        //         var secondCue = connection.secondCue;
        //
        //         var firstCuePosition = TargetTransform.CalculateTargetTransform(firstCue.cue).position;
        //         var secondCuePosition = TargetTransform.CalculateTargetTransform(secondCue.cue).position;
        //         
        //         Gizmos.color = Color.green;
        //         Gizmos.DrawLine(firstCuePosition, secondCuePosition);
        //     }
        // }

        public void OnTimeUpdated(float time)
        {
            foreach (var activeConnector in activeConnectors)
            {
                activeConnector.UpdateConnector();
            }
        }
    }
}