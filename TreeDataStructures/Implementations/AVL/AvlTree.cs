using TreeDataStructures.Core;


namespace TreeDataStructures.Implementations.AVL;

public class AvlTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, AvlNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    private int Height(AvlNode<TKey, TValue>? node)
    {
        if (node == null) return 0;
        return node.Height;
    }

    private void UpdateHeight(AvlNode<TKey, TValue> node)
    {
        node.Height = 1 + Math.Max(Height(node.Left), Height(node.Right));
    }

    private int BalanceFactor(AvlNode<TKey, TValue> node)
    {
        return Height(node.Left) - Height(node.Right);
    }

    private AvlNode<TKey, TValue>? Balance(AvlNode<TKey, TValue>? node)
    {
        if (node == null) return null;
        UpdateHeight(node);
        int cur = BalanceFactor(node);
        if (cur > 1)
        {
            int child = BalanceFactor(node.Left);
            if (child >= 0)//LL
            {
                RotateRight(node);
                UpdateHeight(node);
                UpdateHeight(node.Parent);
                return node.Parent;
            }
            else //LR
            {
                RotateLeft(node.Left);
                RotateRight(node);
                UpdateHeight(node);
                UpdateHeight(node.Parent);
                return node.Parent;
            }
        }
        else if (cur < -1)
        {
            int child = BalanceFactor(node.Right);
            if (child > 0)//RL
            {
                RotateRight(node.Right);
                RotateLeft(node);
                UpdateHeight(node);
                UpdateHeight(node.Parent);
                return node.Parent;
            }
            else //RR
            {
                RotateLeft(node);
                UpdateHeight(node);
                UpdateHeight(node.Parent);
                return node.Parent;
            }
        }
        return node;
    }

    protected override AvlNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new(key, value);
    
    protected override void OnNodeAdded(AvlNode<TKey, TValue> newNode)
    {
        var cur = newNode.Parent;
        while (cur != null)
        {
            var balanced = Balance(cur);
            if (balanced.Parent == null)
                Root = balanced;
            cur = balanced.Parent;
        }
    }

    
}