namespace SmartX.Core.Topology.DataStructures;

/// <summary>
/// A self-balancing timestamp index.
/// Create one tree per sensor.
/// </summary>
public sealed class TelemetryHistoryTree<T>
{
    private Node? _root;

    public int Count { get; private set; }

    public int Height => GetHeight(_root);

    public void Insert(DateTime timestamp, T value)
    {
        _root = InsertNode(_root, timestamp, value);
        Count++;
    }

    /// <summary>
    /// Returns values chronologically, including both boundaries.
    /// Values sharing a timestamp retain their insertion order.
    /// </summary>
    public IReadOnlyList<T> QueryRange(
        DateTime fromUtc,
        DateTime toUtc)
    {
        if (fromUtc > toUtc)
        {
            throw new ArgumentException(
                "The start timestamp must not exceed the end timestamp.");
        }

        var results = new List<T>();

        CollectRange(_root, fromUtc, toUtc, results);

        return results;
    }

    private static Node InsertNode(
        Node? node,
        DateTime timestamp,
        T value)
    {
        if (node is null)
        {
            return new Node(timestamp, value);
        }

        if (timestamp < node.Timestamp)
        {
            node.Left = InsertNode(
                node.Left, timestamp, value);
        }
        else if (timestamp > node.Timestamp)
        {
            node.Right = InsertNode(
                node.Right, timestamp, value);
        }
        else
        {
            // Multiple packets may have the same capture timestamp.
            node.Values.Add(value);
            return node;
        }

        UpdateHeight(node);

        var balance = GetBalance(node);

        if (balance > 1)
        {
            if (GetBalance(node.Left!) < 0)
            {
                node.Left = RotateLeft(node.Left!);
            }

            return RotateRight(node);
        }

        if (balance < -1)
        {
            if (GetBalance(node.Right!) > 0)
            {
                node.Right = RotateRight(node.Right!);
            }

            return RotateLeft(node);
        }

        return node;
    }

    private static void CollectRange(
        Node? node,
        DateTime fromUtc,
        DateTime toUtc,
        List<T> results)
    {
        if (node is null)
        {
            return;
        }

        // Earlier timestamps cannot match when the lower
        // boundary is at or beyond this node.
        if (fromUtc < node.Timestamp)
        {
            CollectRange(
                node.Left, fromUtc, toUtc, results);
        }

        if (node.Timestamp >= fromUtc &&
            node.Timestamp <= toUtc)
        {
            results.AddRange(node.Values);
        }

        // Later timestamps cannot match when the upper
        // boundary is at or before this node.
        if (toUtc > node.Timestamp)
        {
            CollectRange(
                node.Right, fromUtc, toUtc, results);
        }
    }

    private static Node RotateRight(Node node)
    {
        var replacement = node.Left!;
        var middleSubtree = replacement.Right;

        replacement.Right = node;
        node.Left = middleSubtree;

        UpdateHeight(node);
        UpdateHeight(replacement);

        return replacement;
    }

    private static Node RotateLeft(Node node)
    {
        var replacement = node.Right!;
        var middleSubtree = replacement.Left;

        replacement.Left = node;
        node.Right = middleSubtree;

        UpdateHeight(node);
        UpdateHeight(replacement);

        return replacement;
    }

    private static int GetHeight(Node? node)
    {
        return node?.Height ?? 0;
    }

    private static int GetBalance(Node node)
    {
        return GetHeight(node.Left) -
            GetHeight(node.Right);
    }

    private static void UpdateHeight(Node node)
    {
        node.Height = 1 + Math.Max(
            GetHeight(node.Left),
            GetHeight(node.Right));
    }

    private sealed class Node
    {
        public Node(DateTime timestamp, T value)
        {
            Timestamp = timestamp;
            Values.Add(value);
        }

        public DateTime Timestamp { get; }

        public List<T> Values { get; } = [];

        public Node? Left { get; set; }

        public Node? Right { get; set; }

        public int Height { get; set; } = 1;
    }
}

//Code attributes

/*
Title: AVL Tree Data Structure
Author(s): GeeksforGeeks
Date: 2025
Version: Not applicable
Type: Online tutorial
Availability: https://www.geeksforgeeks.org/dsa/introduction-to-avl-tree/
*/