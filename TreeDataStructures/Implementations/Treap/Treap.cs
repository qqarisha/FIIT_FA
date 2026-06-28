using System.ComponentModel.Design.Serialization;
using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.Treap;

public class Treap<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, TreapNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    /// <summary>
    /// Разрезает дерево с корнем <paramref name="root"/> на два поддерева:
    /// Left: все ключи <= <paramref name="key"/>
    /// Right: все ключи > <paramref name="key"/>
    /// </summary>
    protected virtual (TreapNode<TKey, TValue>? Left, TreapNode<TKey, TValue>? Right) Split(TreapNode<TKey, TValue>? root, TKey key)
    {
        if (root == null)
        {
            return (null, null);
        }
        int cmp = Comparer.Compare(root.Key, key);
        if (cmp <= 0)
        {

            var (a, b) = Split(root.Right, key);
            root.Right = a;
            if (a != null) a.Parent = root;
            return (root, b);
        }
        else
        {
            var (a, b) = Split(root.Left, key);
            root.Left = b;
            if (b != null) b.Parent = root;
            return (a, root);
        }
    }

    /// <summary>
    /// Сливает два дерева в одно.
    /// Важное условие: все ключи в <paramref name="left"/> должны быть меньше ключей в <paramref name="right"/>.
    /// Слияние происходит на основе Priority (куча).
    /// </summary>
    protected virtual TreapNode<TKey, TValue>? Merge(TreapNode<TKey, TValue>? left, TreapNode<TKey, TValue>? right)
    {
        if (left == null) return right;
        if (right == null) return left;
        if (left.Priority < right.Priority) //меньший приоритет у корня
        {
            left.Right = Merge(left.Right, right);
            if (left.Right != null) left.Right.Parent = left;
            return left;
        }
        else
        {
            right.Left = Merge(left, right.Left);
            if (right.Left != null) right.Left.Parent = right;
            return right;
        }
    }
    

    public override void Add(TKey key, TValue value)
    {
        TreapNode<TKey, TValue>? oldnode = FindNode(key);
        if (oldnode != null)
        {
            oldnode.Value = value;
            return;
        }
        var node = CreateNode(key, value);
        if (Root == null)
        {
            Root = node;
            Count++;
            OnNodeAdded(node);
            return;
        }
        var (left, right) = Split(Root, key);
        var merged = Merge(left, node);
        Root = Merge(merged, right);
        if (Root != null)
            Root.Parent = null;
        Count++;
        
        OnNodeAdded(node);
    }

    private TreapNode<TKey, TValue>? RemoveTreap(TreapNode<TKey, TValue>? root, TKey key)
    {
        if (root == null)
            return null;
        int cmp = Comparer.Compare(key, root.Key);
        if (cmp < 0)
        {
            root.Left = RemoveTreap(root.Left, key);
            if (root.Left != null) root.Left.Parent = root;
            return root;
        }
        else if (cmp > 0)
        {
            root.Right = RemoveTreap(root.Right, key);
            if (root.Right != null) root.Right.Parent = root;
            return root;
        }
        else
        {
            var merged = Merge(root.Left, root.Right);
            if (merged != null) merged.Parent = root.Parent;
            return merged;
        }
    }


    public override bool Remove(TKey key)
    {
        if (!ContainsKey(key)) return false;
        Root = RemoveTreap(Root, key);
        if (Root != null) Root.Parent = null;
        Count--;
        return true;
    }


    protected override TreapNode<TKey, TValue> CreateNode(TKey key, TValue value)
    {
        var node = new TreapNode<TKey, TValue>(key, value);
        return node;
    }
    protected override void OnNodeAdded(TreapNode<TKey, TValue> newNode)
    {
        
    }
    
    protected override void OnNodeRemoved(TreapNode<TKey, TValue>? parent, TreapNode<TKey, TValue>? child)
    {
        
    }
    
}