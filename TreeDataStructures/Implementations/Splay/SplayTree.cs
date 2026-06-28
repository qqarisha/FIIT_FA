using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Implementations.BST;

namespace TreeDataStructures.Implementations.Splay;

public class SplayTree<TKey, TValue> : BinarySearchTree<TKey, TValue>
    where TKey : IComparable<TKey>
{
    protected override BstNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new(key, value);

    private void splay(BstNode<TKey, TValue> node)
    {
        while (node != Root)
        {
            var parent = node.Parent;
            var grand = parent.Parent;
            if (parent == Root)
            {
                zig(node);
            }
            else if ((node == parent.Left && parent == grand.Left) || (node == parent.Right && parent == grand.Right))
            {
                zig_zig(node);
            }
            else
            {
                zig_zag(node);
            }
        }
    }

        private void zig(BstNode<TKey, TValue> node)
    {
        var parent = node.Parent;
        if (node == parent.Left)
        {
            RotateRight(parent);
        }
        else
        {
            RotateLeft(parent);
        }
    }


    private void zig_zig(BstNode<TKey, TValue> node)
    {
        var parent = node.Parent;
        var grand = parent.Parent;
        if (node == parent.Left && parent == grand.Left)
        {
            RotateRight(grand);
            RotateRight(node.Parent);
        }
        else
        {
            RotateLeft(grand);
            RotateLeft(node.Parent);
        }
    }

    private void zig_zag(BstNode<TKey, TValue> node)
    {
        var parent = node.Parent;
        var grand = parent.Parent;
        if (parent == grand.Left && node == parent.Right)
        {
            RotateLeft(parent);
            RotateRight(grand);
        }
        else
        {
            RotateRight(parent);
            RotateLeft(grand);
        }
    }
    
    protected override void OnNodeAdded(BstNode<TKey, TValue> newNode)
    {
        splay(newNode);
    }
    
    protected override void OnNodeRemoved(BstNode<TKey, TValue>? parent, BstNode<TKey, TValue>? child)
    {
        // if (child != null)
        // {
        //     splay(child);
        // }
        // else if (parent != null)
        // {
        //     splay(parent);
        // }
    }

    public override bool ContainsKey(TKey key)
    {
        return TryGetValue(key, out _);
    }

    
    public override bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        var cur = Root;
        BstNode<TKey, TValue>? lastVisit = null;
        while (cur != null)
        {
            lastVisit = cur;
            int cmp = Comparer.Compare(key, cur.Key);
            if (cmp < 0)
            {
                cur = cur.Left;
            }
            else if (cmp > 0)
            {
                cur = cur.Right;
            }
            else
            {
                value = cur.Value;
                splay(cur);
                return true;
            }
        }
        value = default;
        if (lastVisit != null) splay(lastVisit);
        return false;
    }   
}
