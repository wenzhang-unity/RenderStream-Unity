using UnityEngine.UIElements;

namespace Disguise.RenderStream.Parameters
{
    partial class ParameterTreeView
    {
        interface ICell
        {
            void Initialize(ParameterTreeView treeView);
            void Bind(ItemData data);
            void Unbind();
        }
        
        abstract class ParameterTreeViewColumn<T> : Column where T : VisualElement, ICell, new()
        {
            protected readonly ParameterTreeView m_TreeView;

            protected ParameterTreeViewColumn(ParameterTreeView treeView)
            {
                m_TreeView = treeView;

                makeCell = MakeCell;
                bindCell = BindCell;
                unbindCell = UnbindCell;
                destroyCell = DestroyCell;
            }

            VisualElement MakeCell()
            {
                var cell = new T();
                cell.Initialize(m_TreeView);

                m_TreeView.MakeItem(cell);

                return cell;
            }

            void BindCell(VisualElement ve, int index)
            {
                var data = m_TreeView.GetItemDataForIndex<ItemData>(index);

                m_TreeView.BindItem(ve, index);

                var cell = (ICell)ve;
                cell.Bind(data);
            }

            void UnbindCell(VisualElement ve, int index)
            {
                var cell = (ICell)ve;
                cell.Unbind();

                m_TreeView.UnbindItem(ve, index);
            }

            void DestroyCell(VisualElement ve)
            {
                m_TreeView.DestroyItem(ve);
            }
        }
    }
}

