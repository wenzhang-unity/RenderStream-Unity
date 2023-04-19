using UnityEngine.UIElements;

namespace Disguise.RenderStream.Parameters
{
    partial class ParameterTreeView
    {
        /// <summary>
        /// Represents a cell in a <see cref="ParameterTreeViewColumn{T}"/>.
        /// </summary>
        interface ICell
        {
            /// <summary>
            /// Initialize for the given view.
            /// </summary>
            void Initialize(ParameterTreeView treeView);
            
            /// <summary>
            /// Called on a new or recycled instance to attach it to row data.
            /// </summary>
            void Bind(ItemData data);
            
            /// <summary>
            /// Called when this instance is destroyed or recycled to detach it from its row data.
            /// </summary>
            void Unbind();
        }
        
        /// <summary>
        /// Represents a column in a <see cref="ParameterTreeView"/>.
        /// </summary>
        abstract class ParameterTreeViewColumn<T> : Column where T : VisualElement, ICell, new()
        {
            readonly ParameterTreeView m_TreeView;

            protected ParameterTreeViewColumn(ParameterTreeView treeView)
            {
                m_TreeView = treeView;

                makeCell = MakeCell;
                bindCell = BindCell;
                unbindCell = UnbindCell;
                destroyCell = DestroyCell;
            }

            /// <inheritdoc cref="Column.makeCell"/>
            VisualElement MakeCell()
            {
                var cell = new T();
                cell.Initialize(m_TreeView);

                m_TreeView.MakeItem(cell); // For TreeViewExtended

                return cell;
            }

            /// <inheritdoc cref="Column.bindCell"/>
            void BindCell(VisualElement ve, int index)
            {
                var data = m_TreeView.GetItemDataForIndex<ItemData>(index);

                m_TreeView.BindItem(ve, index); // For TreeViewExtended

                var cell = (ICell)ve;
                cell.Bind(data);
            }

            /// <inheritdoc cref="Column.unbindCell"/>
            void UnbindCell(VisualElement ve, int index)
            {
                var cell = (ICell)ve;
                cell.Unbind();

                m_TreeView.UnbindItem(ve, index); // For TreeViewExtended
            }

            /// <inheritdoc cref="Column.destroyCell"/>
            void DestroyCell(VisualElement ve)
            {
                m_TreeView.DestroyItem(ve); // For TreeViewExtended
            }
        }
    }
}

