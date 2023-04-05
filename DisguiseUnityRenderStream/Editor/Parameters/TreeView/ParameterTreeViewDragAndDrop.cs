using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Disguise.RenderStream.Parameters
{
    partial class ParameterTreeView
    {
        /// <summary>
        /// Implements drag & dropping a parameter into the scene view to select its GameObject and frame it.
        /// </summary>
        static DragAndDropVisualMode SceneDropHandler(UnityEngine.Object dropUpon, Vector3 worldPosition, Vector2 viewportPosition, Transform parentForDraggedObjects, bool perform)
        {
            var data = DragAndDrop.GetGenericData(Contents.DragDataKey);
            
            if (data is IList<object> { Count: 1 } dataList &&
                dataList[0] is Parameter { Object: GameObject gameObject })
            {
                if (perform)
                {
                    Selection.activeGameObject = gameObject;
                    SceneView.FrameLastActiveSceneView();
                    
                }
                
                return DragAndDropVisualMode.Link;
            }
            
            return DragAndDropVisualMode.None;
        }
        
        /// <summary>
        /// Prevents drag & drop when the default group is selected.
        /// </summary>
        protected override bool CanStartDrag(CanStartDragArgs args)
        {
            var items = FindRows(args.draggedItemIDs);
            
            foreach (var item in items)
            {
                // Cannot re-order the default group
                if (item is ParameterGroupTreeViewItem { Group: { IsDefaultGroup: true } })
                    return false;
            }

            return true;
        }
        
        protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
            DragAndDrop.PrepareStartDrag();
        
            var sortedDraggedIDs = SortItemIDsInRowOrder(args.draggedItemIDs);
            var genericData = FindRows(sortedDraggedIDs).Select(new Func<TreeViewItem, object>(item =>
            {
                if (item is ParameterGroupTreeViewItem groupItem)
                    return groupItem.Group;
                else if (item is ParameterTreeViewItem parameterItem)
                    return parameterItem.Parameter;
                else
                    throw new NotImplementedException();
            }));

            DragAndDrop.SetGenericData(Contents.DragDataKey, genericData.ToArray());
        
            DragAndDrop.StartDrag(Contents.DragTitle);
        }
        
        /// <summary>
        /// Receives drag & drop objects, which may originate from any Unity window.
        /// </summary>
        /// 
        /// <remarks>
        /// This function is also called while dragging (<see cref="TreeView.DragAndDropArgs.performDrop"/> will be false until the items are dropped)
        /// to check if the target is valid and to update the cursor icon.
        /// </remarks>
        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            // TODO this is messy, break into smaller functions
            
            if (args.performDrop)
            {
                var newSelection = Array.Empty<int>();
                
                switch (args.dragAndDropPosition)
                {
                    case DragAndDropPosition.UponItem:
                        if (DragAndDrop.objectReferences.Length > 0)
                        {
                            RegisterUndo(Contents.UndoDragAndDropAssignParameters);

                            if (args.parentItem is ParameterGroupTreeViewItem groupItem)
                            {
                                var insertAtIndex = args.insertAtIndex;
                                
                                newSelection = new int[DragAndDrop.objectReferences.Length];
                                for (var i = 0; i < DragAndDrop.objectReferences.Length; i++)
                                {
                                    var obj = DragAndDrop.objectReferences[i];
                                    newSelection[i] = AddGenericObjectToGroup(groupItem.Group, obj, insertAtIndex++).ID;
                                }
                            }
                            else if (args.parentItem is ParameterTreeViewItem parameterItem)
                            {
                                var firstObject = DragAndDrop.objectReferences[0];
                                AssignGenericObjectToParameter(parameterItem.Parameter, firstObject);
                                newSelection = new []{ parameterItem.Parameter.ID };
                            }
                            else
                            {
                                throw new NotImplementedException();
                            }
                        }
                        
                        break;

                    case DragAndDropPosition.BetweenItems:
                        if (DragAndDrop.objectReferences.Length > 0)
                        {
                            RegisterUndo(Contents.UndoDragAndDropNewParameters);

                            if (args.parentItem == rootItem)
                            {
                                var newGroup = new ParameterGroup
                                {
                                    Name = GetUniqueGroupName(Contents.NewGroupName),
                                    ID = m_ParameterList.ReserveID()
                                };
                                
                                newSelection = new int[DragAndDrop.objectReferences.Length];
                                for (var i = 0; i < DragAndDrop.objectReferences.Length; i++)
                                {
                                    var obj = DragAndDrop.objectReferences[i];
                                    newSelection[i] = AddGenericObjectToGroup(newGroup, obj).ID;
                                }
                            
                                m_ParameterList.m_Groups.Insert(args.insertAtIndex, newGroup);
                            }
                            else if (args.parentItem is ParameterGroupTreeViewItem groupItem)
                            {
                                var insertAtIndex = args.insertAtIndex;
                                
                                newSelection = new int[DragAndDrop.objectReferences.Length];
                                for (var i = 0; i < DragAndDrop.objectReferences.Length; i++)
                                {
                                    var obj = DragAndDrop.objectReferences[i];
                                    newSelection[i] = AddGenericObjectToGroup(groupItem.Group, obj, insertAtIndex++).ID;
                                }
                            }
                            else
                            {
                                throw new NotImplementedException();
                            }
                        }
                        
                        break;
        
                    case DragAndDropPosition.OutsideItems:
                        if (DragAndDrop.objectReferences.Length > 0)
                        {
                            RegisterUndo(Contents.UndoDragAndDropNewParameters);
                            
                            var newGroup = new ParameterGroup
                            {
                                Name = GetUniqueGroupName(Contents.NewGroupName),
                                ID = m_ParameterList.ReserveID()
                            };

                            newSelection = new int[DragAndDrop.objectReferences.Length];
                            for (var i = 0; i < DragAndDrop.objectReferences.Length; i++)
                            {
                                var obj = DragAndDrop.objectReferences[i];
                                newSelection[i] = AddGenericObjectToGroup(newGroup, obj).ID;
                            }
                            
                            m_ParameterList.m_Groups.Add(newGroup);
                        }
                        
                        break;
        
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (newSelection.Length > 0)
                {
                    Reload();
                    
                    SelectRevealAndFrame(newSelection);
                }
                
                return DragAndDropVisualMode.Move;
            }
            
            switch (args.dragAndDropPosition)
            {
                case DragAndDropPosition.UponItem:
                    if (DragAndDrop.objectReferences.Length > 0)
                    {
                        if (args.parentItem is ParameterGroupTreeViewItem)
                        {
                            return DragAndDropVisualMode.Copy;
                        }
                        
                        if (args.parentItem is ParameterTreeViewItem)
                        {
                            if (DragAndDrop.objectReferences.Length == 1)
                                return DragAndDropVisualMode.Link;
                        }
                    }
                    
                    break;

                case DragAndDropPosition.BetweenItems:
                    if (DragAndDrop.objectReferences.Length > 0)
                    {
                        if (args.parentItem == rootItem)
                        {
                            return DragAndDropVisualMode.Copy;
                        }
                        
                        if (args.parentItem is ParameterGroupTreeViewItem)
                        {
                            return DragAndDropVisualMode.Copy;
                        }
                    }
                    
                    break;
    
                case DragAndDropPosition.OutsideItems:
                    if (DragAndDrop.objectReferences.Length > 0)
                    {
                        return DragAndDropVisualMode.Copy;
                    }
                    
                    break;
    
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            return DragAndDropVisualMode.Rejected;
        }
        
        /// <summary>
        /// Declares all tree rows to be capable of receiving drag & drop items.
        /// More specific filtering is done in <see cref="HandleDragAndDrop"/>.
        /// </summary>
        protected override bool CanBeParent(TreeViewItem item)
        {
            return true;
        }
        
        Parameter AddGenericObjectToGroup(ParameterGroup group, UnityEngine.Object obj, int insertAtIndex = -1)
        {
            var newParameter = new Parameter
            {
                ID = m_ParameterList.ReserveID()
            };
            AssignGenericObjectToParameter(newParameter, obj);
            
            newParameter.AutoAssignName();
            newParameter.Name = GetUniqueParameterName(newParameter.Name, group);
            
            if (insertAtIndex > 0)
                group.m_Parameters.Insert(insertAtIndex, newParameter);
            else
                group.m_Parameters.Add(newParameter);

            return newParameter;
        }

        void AssignGenericObjectToParameter(Parameter parameter, UnityEngine.Object obj)
        {
            if (obj is Component component)
            {
                parameter.Component = component;
            }
            else
            {
                parameter.Object = obj;
            }
        }
    }
}
