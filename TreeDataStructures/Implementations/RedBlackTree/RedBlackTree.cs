using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.RedBlackTree;

public class RedBlackTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, RbNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    protected override RbNode<TKey, TValue> CreateNode(TKey key, TValue value)
    {
        return new RbNode<TKey, TValue>(key, value);
    }

    protected override void OnNodeAdded(RbNode<TKey, TValue> newNode)
    {
        if (newNode.Parent == null)
        {
            newNode.Color = RbColor.Black;
            return;
        }
        if (newNode.Parent.Color == RbColor.Black)
        {
            
        }
        else
        {
            var problemNode = newNode;
            while (problemNode.Parent != null && problemNode.Parent.Color == RbColor.Red)
            {
                var parent = problemNode.Parent;
                if (problemNode.Parent.Parent == null)
                {
                    problemNode.Parent.Color = RbColor.Black;
                    break;
                }
                var grand = parent.Parent;
                var uncle = (grand.Left == parent) ? grand.Right : grand.Left;
                if (uncle != null && uncle.Color == RbColor.Red)
                {
                    grand.Color = RbColor.Red;
                    parent.Color = RbColor.Black;
                    uncle.Color = RbColor.Black;
                    problemNode = grand;
                }
                else
                {
                    if (grand.Left == parent)
                    {
                        if (parent.Right  == problemNode) 
                        {
                            RotateLeft(parent);
                            problemNode = parent;
                            parent = problemNode.Parent;
                            grand = parent.Parent;
                        }
                        parent.Color = RbColor.Black;
                        grand.Color = RbColor.Red;
                        RotateRight(grand);
                    }
                    else
                    {
                        if (parent.Left  == problemNode)
                        { 
                            RotateRight(parent);
                            problemNode = parent;
                            parent = problemNode.Parent;
                            grand = parent.Parent;
                        }
                        parent.Color = RbColor.Black;
                        grand.Color = RbColor.Red;
                        RotateLeft(grand);
                    }
                }
            }
        }
    }
    protected override void OnNodeRemoved(RbNode<TKey, TValue>? parent, RbNode<TKey, TValue>? child)
    {
        if (child != null && child.Color == RbColor.Red)
        {
            child.Color = RbColor.Black;
            return;
        }
        while (child != Root && (child == null || child.Color == RbColor.Black))
        {
            if (parent == null) break;
            if ((child == parent.Left) || (child == null && parent.Left == null))
            {
                var sibling = parent.Right;
                //случай 1
                if (sibling != null && sibling.Color == RbColor.Red)
                {
                    sibling.Color = RbColor.Black;
                    parent.Color = RbColor.Red;
                    RotateLeft(parent);
                    sibling = parent.Right;
                }
                //случай 2
                if (sibling != null && sibling.Color == RbColor.Black)
                {
                    var left = sibling.Left;
                    var right = sibling.Right;

                    if ((left == null || left.Color == RbColor.Black) &&
                        (right == null || right.Color == RbColor.Black))
                    {
                        sibling.Color = RbColor.Red;
                        child = parent;
                        parent = parent.Parent;
                        continue;
                    }
                }
                //случай 3
                if (sibling != null && sibling.Color == RbColor.Black)
                {
                    var left = sibling.Left;
                    var right = sibling.Right;

                    if (left != null && left.Color == RbColor.Red &&
                        (right == null || right.Color == RbColor.Black))
                    {
                        sibling.Color = RbColor.Red;
                        left.Color = RbColor.Black;
                        RotateRight(sibling);
                        sibling = parent.Right;
                        continue;
                    }
                }
                //случай 4
                if (sibling != null && sibling.Color == RbColor.Black)
                {
                    var right = sibling.Right;
                    if (right != null && right.Color == RbColor.Red)
                    {
                        sibling.Color = parent.Color;
                        parent.Color = RbColor.Black;
                        right.Color = RbColor.Black;
                        RotateLeft(parent);
                        break;
                    }
                }
            }
            else
            {
                //случай 1
                var sibling = parent.Left;
                if (sibling != null && sibling.Color == RbColor.Red)
                {
                    sibling.Color = RbColor.Black;
                    parent.Color = RbColor.Red;
                    RotateRight(parent);
                    sibling = parent.Left;
                }
                // случай 2
                if (sibling != null && sibling.Color == RbColor.Black)
                {
                    var left = sibling.Left;
                    var right = sibling.Right;

                    if ((right == null || right.Color == RbColor.Black) &&
                        (left == null || left.Color == RbColor.Black))
                    {
                        sibling.Color = RbColor.Red;
                        child = parent;
                        parent = parent.Parent;
                        continue;
                    }
                }
                //случай 3
                if (sibling != null && sibling.Color == RbColor.Black)
                {
                    var left = sibling.Left;
                    var right = sibling.Right;

                    if (right != null && right.Color == RbColor.Red &&
                        (left == null || left.Color == RbColor.Black))
                    {
                        sibling.Color = RbColor.Red;
                        right.Color = RbColor.Black;
                        RotateLeft(sibling);
                        sibling = parent.Left;
                        continue;
                    }
                }
                //случай 4
                if (sibling != null && sibling.Color == RbColor.Black)
                {
                    var left = sibling.Left;

                    if (left != null && left.Color == RbColor.Red)
                    {
                        sibling.Color = parent.Color;
                        parent.Color = RbColor.Black;
                        left.Color = RbColor.Black;
                        RotateRight(parent);
                        break;
                    }
                }
            }
            break;
        }
        if (child != null) child.Color = RbColor.Black;
    }

}