using System.Collections;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics.CodeAnalysis;
using System.Security.AccessControl;
using TreeDataStructures.Interfaces;

namespace TreeDataStructures.Core;

public abstract class BinarySearchTreeBase<TKey, TValue, TNode>(IComparer<TKey>? comparer = null) 
    : ITree<TKey, TValue>
    where TNode : Node<TKey, TValue, TNode>
{
    protected TNode? Root;
    public IComparer<TKey> Comparer { get; protected set; } = comparer ?? Comparer<TKey>.Default; // use it to compare Keys

    public int Count { get; protected set; }
    
    public bool IsReadOnly => false;

    public ICollection<TKey> Keys => InOrder().Select(x => x.Key).ToList();
    public ICollection<TValue> Values => InOrder().Select(x => x.Value).ToList();
    
    
    public virtual void Add(TKey key, TValue value)
    {
        TNode newNode = CreateNode(key, value);
        if (Root == null)
        {
            Root = newNode;
            Count++;
            OnNodeAdded(newNode);
            return;
        }
        else
        {
            TNode? cur = Root;
            while (true)
            {
                int cmp = Comparer.Compare(key, cur.Key);
                if (cmp == 0)
                {
                    cur.Value = value;
                    return;
                }
                if (cmp < 0)
                {
                    if (cur.Left == null)
                    {
                        cur.Left = newNode;
                        newNode.Parent = cur;
                        break;
                    }
                    cur = cur.Left;
                }
                if (cmp > 0)
                {
                    if (cur.Right == null)
                    {
                        cur.Right = newNode;
                        newNode.Parent = cur;
                        break;
                    }
                    cur = cur.Right;
                }
            }
            Count++;
            OnNodeAdded(newNode);
        }

    }

    
    public virtual bool Remove(TKey key)
    {
        TNode? node = FindNode(key);
        if (node == null) { return false; }

        RemoveNode(node);
        this.Count--;
        return true;
    }
    
    
    protected virtual void RemoveNode(TNode node)
    {
        if (node.Left == null && node.Right == null)
        {
            TNode? parent = node.Parent;
            Transplant(node, null);
            OnNodeRemoved(parent, null);
        }
        else if (node.Left != null && node.Right == null)
        {
            TNode? parent = node.Parent;
            TNode? left = node.Left;
            Transplant(node, left);
            OnNodeRemoved(parent, left);
        }
        else if (node.Left == null && node.Right != null)
        {
            TNode? parent = node.Parent;
            TNode? right = node.Right;
            Transplant(node, right);
            OnNodeRemoved(parent, right);
        }
        else
        {
            TNode? parent = node.Parent;
            TNode? cur = node.Right;
            while (cur.Left != null)
            {
                cur = cur.Left;
            }
            if (cur != node.Right)
            {
                TNode? curParent = cur.Parent;
                Transplant(cur, cur.Right);
                cur.Right = node.Right;
                cur.Right.Parent = cur;
            }
            Transplant(node, cur);
            OnNodeRemoved(parent, cur);
            cur.Left = node.Left;
            cur.Left.Parent = cur;
            
        }
    }

    public virtual bool ContainsKey(TKey key) => FindNode(key) != null;
    
    public virtual bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        TNode? node = FindNode(key);
        if (node != null)
        {
            value = node.Value;
            return true;
        }
        value = default;
        return false;
    }

    public TValue this[TKey key]
    {
        get => TryGetValue(key, out TValue? val) ? val : throw new KeyNotFoundException();
        set => Add(key, value);
    }

    
    #region Hooks
    
    /// <summary>
    /// Вызывается после успешной вставки
    /// </summary>
    /// <param name="newNode">Узел, который встал на место</param>
    protected virtual void OnNodeAdded(TNode newNode) { }
    
    /// <summary>
    /// Вызывается после удаления. 
    /// </summary>
    /// <param name="parent">Узел, чей ребенок изменился</param>
    /// <param name="child">Узел, который встал на место удаленного</param>
    protected virtual void OnNodeRemoved(TNode? parent, TNode? child) { }
    
    #endregion
    
    
    #region Helpers
    protected abstract TNode CreateNode(TKey key, TValue value);
    
    
    protected TNode? FindNode(TKey key)
    {
        TNode? current = Root;
        while (current != null)
        {
            int cmp = Comparer.Compare(key, current.Key);
            if (cmp == 0) { return current; }
            current = cmp < 0 ? current.Left : current.Right;
        }
        return null;
    }

    protected void RotateLeft(TNode x)
    {
        TNode? y = x.Right;
        if (y == null) return;
        TNode? yLeft = y.Left;
        x.Right = yLeft;
        if (yLeft != null)
        {
            yLeft.Parent = x;
        }
        y.Left = x;
        TNode? xParent = x.Parent;
        x.Parent = y;
        y.Parent = xParent;
        if (xParent == null)
        {
            Root = y;
        }
        else if (xParent.Left == x)
        {
            xParent.Left = y;
        }
        else
        {
            xParent.Right = y;
        }
    }

    protected void RotateRight(TNode y)
    {
        TNode? x = y.Left;
        if (x == null) return;
        TNode? xRight = x.Right;
        y.Left = xRight;
        if (xRight != null)
        {
            xRight.Parent = y;
        }
        x.Right = y;
        TNode? yParent = y.Parent;
        y.Parent = x;
        x.Parent = yParent;
        if (yParent == null)
        {
            Root = x;
        }
        else if (yParent.Left == y)
        {
            yParent.Left = x;
        }
        else
        {
            yParent.Right = x;
        }
    }
    
    protected void RotateBigLeft(TNode x)
    {
        RotateRight (x.Right!);
        RotateLeft(x);
    }
    
    protected void RotateBigRight(TNode y)
    {
        RotateLeft (y.Left!);
        RotateRight(y);
    }
    
    protected void RotateDoubleLeft(TNode x)
    {
        RotateLeft(x);
        RotateLeft(x.Parent!);
    }
    
    protected void RotateDoubleRight(TNode y)
    {
        RotateRight(y);
        RotateRight(y.Parent!);
    }
    
    protected void Transplant(TNode u, TNode? v)
    {
        if (u.Parent == null)
        {
            Root = v;
        }
        else if (u.IsLeftChild)
        {
            u.Parent.Left = v;
        }
        else
        {
            u.Parent.Right = v;
        }
        v?.Parent = u.Parent;
    }
    #endregion
    
    public IEnumerable<TreeEntry<TKey, TValue>> InOrder() 
        => new TreeIterator(Root, TraversalStrategy.InOrder);

    public IEnumerable<TreeEntry<TKey, TValue>> PreOrder() 
        => new TreeIterator(Root, TraversalStrategy.PreOrder);

    public IEnumerable<TreeEntry<TKey, TValue>> PostOrder() 
        => new TreeIterator(Root, TraversalStrategy.PostOrder);

    public IEnumerable<TreeEntry<TKey, TValue>> InOrderReverse() 
        => new TreeIterator(Root, TraversalStrategy.InOrderReverse);

    public IEnumerable<TreeEntry<TKey, TValue>> PreOrderReverse() 
        => new TreeIterator(Root, TraversalStrategy.PreOrderReverse);

    public IEnumerable<TreeEntry<TKey, TValue>> PostOrderReverse() 
        => new TreeIterator(Root, TraversalStrategy.PostOrderReverse);

    /// <summary>
    /// Внутренний класс-итератор. 
    /// Реализует паттерн Iterator вручную, без yield return (ban).
    /// </summary>
    private struct TreeIterator : 
        IEnumerable<TreeEntry<TKey, TValue>>,
        IEnumerator<TreeEntry<TKey, TValue>>
    {
        // probably add something here
        private readonly TraversalStrategy _strategy; // or make it template parameter?
        private readonly TNode? _root;
        private TNode? _cur;
        private bool _start;

        public TreeIterator(TNode? root, TraversalStrategy strat)
        {
            _root = root;
            _strategy = strat;
            _cur = null;
            _start = false;
        }
        
        public IEnumerator<TreeEntry<TKey, TValue>> GetEnumerator() => this;
        IEnumerator IEnumerable.GetEnumerator() => this;
        
        public TreeEntry<TKey, TValue> Current => _cur == null ? throw new InvalidOperationException()
        : new TreeEntry<TKey, TValue>(_cur.Key, _cur.Value, _cur.Height);
        object IEnumerator.Current => Current;
        
        
        public bool MoveNext()
        {
            if (_strategy == TraversalStrategy.InOrder) //лево - корнеь - право
            {
                if (!_start)
                {
                    _start = true;
                    _cur = _root;
                    if (_root == null) return false;
                    while(_cur.Left != null)
                    {
                        _cur = _cur.Left;
                    }
                    return true;
                }
                else
                {
                    if (_cur.Right != null)
                    {
                        _cur = _cur.Right;
                        while(_cur.Left != null)
                        {
                            _cur = _cur.Left;
                        }
                        return true;
                    }
                    else
                    {
                        if (_cur.Parent == null) return false;
                        while (_cur.Parent != null && _cur.Parent.Right == _cur)
                        {
                            _cur = _cur.Parent;
                        }
                        if (_cur.Parent != null){
                        _cur = _cur.Parent;
                        }
                        else
                        {
                            return false;
                        }
                        return true;
                    }
                }
            }
            else if (_strategy == TraversalStrategy.InOrderReverse)//право - корень - лево
            {
                if (!_start)
                {
                    _start = true;
                    _cur = _root;
                    if (_root == null) return false;
                    while(_cur.Right != null)
                    {
                        _cur = _cur.Right;
                    }
                    return true;
                }
                else
                {
                    if (_cur.Left != null)
                    {
                        _cur = _cur.Left;
                        while(_cur.Right != null)
                        {
                            _cur = _cur.Right;
                        }
                        return true;
                    }
                    else
                    {
                        if (_cur.Parent == null) return false;
                        while (_cur.Parent != null && _cur.Parent.Left == _cur)
                        {
                            _cur = _cur.Parent;
                        }
                        if (_cur.Parent != null){
                        _cur = _cur.Parent;
                        }
                        else
                        {
                            return false;
                        }
                        return true;
                    }
                }
            }
            else if (_strategy == TraversalStrategy.PreOrder)//корень - лево - право
            {
                if (!_start)
                {
                    _start = true;
                    _cur = _root;
                    if (_root == null) return false;
                    return true;
                }

                if (_cur.Left != null)
                {
                    _cur = _cur.Left;
                    return true;
                }

                if (_cur.Right != null)
                {
                    _cur = _cur.Right;
                    return true;
                }

                while (_cur.Parent != null)
                {
                    if (_cur == _cur.Parent.Left && _cur.Parent.Right != null)
                    {
                        _cur = _cur.Parent.Right;
                        return true;
                    }
                    _cur = _cur.Parent;
                }

                return false;
            }
            if (_strategy == TraversalStrategy.PreOrderReverse) // право - лево - корень
            {
                if (!_start)
                {
                    if (_root != null)
                    {
                        _cur = _root;
                        while (_cur.Left != null || _cur.Right != null)
                        {
                            if (_cur.Right != null) _cur = _cur.Right;
                            else _cur = _cur.Left;
                        }
                        _start = true;
                        return true;
                    }
                    return false;
                }

                if (_cur == null) return false;

                TNode? parent = _cur.Parent;

                if (parent == null)
                {
                    _cur = null;
                    return false;
                }

                if (_cur == parent.Right && parent.Left != null)
                {
                    _cur = parent.Left;
                    while (_cur.Right != null || _cur.Left != null)
                    {
                        if (_cur.Right != null) _cur = _cur.Right;
                        else _cur = _cur.Left;
                    }
                    return true;
                }
                else
                {
                    _cur = parent;
                    return true;
                }
            }

            else if (_strategy == TraversalStrategy.PostOrder) //лево - право - корень
            {
                if (!_start)
                {
                    _start = true;
                    _cur = _root;
                    if (_cur == null) return false;

                    while (true)
                    {
                        if (_cur.Left != null)
                            _cur = _cur.Left;
                        else if (_cur.Right != null)
                            _cur = _cur.Right;
                        else
                            return true;
                    }
                }

                if (_cur.Parent == null) return false;

                if (_cur == _cur.Parent.Left && _cur.Parent.Right != null)
                {
                    _cur = _cur.Parent.Right;

                    while (true)
                    {
                        if (_cur.Left != null)
                            _cur = _cur.Left;
                        else if (_cur.Right != null)
                            _cur = _cur.Right;
                        else
                            return true;
                    }
                }

                _cur = _cur.Parent;
                return true;
            }
            if (_strategy == TraversalStrategy.PostOrderReverse) // корень - право - лево
            {
                if (!_start)
                {
                    if (_root != null)
                    {
                        _cur = _root;
                        _start = true;
                        return true;
                    }
                    return false;
                }

                if (_cur == null) return false;

                if (_cur.Right != null)
                {
                    _cur = _cur.Right;
                    return true;
                }
                if (_cur.Left != null)
                {
                    _cur = _cur.Left;
                    return true;
                }

                TNode? child = _cur;
                TNode? parent = _cur.Parent;
                while (parent != null && (child == parent.Left || parent.Left == null))
                {
                    child = parent;
                    parent = parent.Parent;
                }

                if (parent == null)
                {
                    _cur = null;
                    return false;
                }
                _cur = parent.Left;
                return true;
            }

            throw new NotImplementedException("Strategy not implemented");
        }
        
        public void Reset()
        {
            _cur = null;
            _start = false;
        }

        
        public void Dispose()
        {
            // TODO release managed resources here
        }
    }
    
    
    private enum TraversalStrategy { InOrder, PreOrder, PostOrder, InOrderReverse, PreOrderReverse, PostOrderReverse }
    
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        throw new NotImplementedException();
    }
    
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
    public void Clear() { Root = null; Count = 0; }
    public bool Contains(KeyValuePair<TKey, TValue> item) => ContainsKey(item.Key);
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
         if (array == null) throw new ArgumentNullException(nameof(array));

        if (arrayIndex < 0) throw new ArgumentOutOfRangeException(nameof(arrayIndex));

        if (array.Length - arrayIndex < Count) throw new ArgumentException("array is small");

        foreach (var entry in InOrder())
        {
            array[arrayIndex++] = new KeyValuePair<TKey, TValue>(entry.Key, entry.Value);
        }
    }
    public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);
}