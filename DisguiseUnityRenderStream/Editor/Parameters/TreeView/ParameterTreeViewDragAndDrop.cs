using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Disguise.RenderStream.Parameters
{
    partial class ParameterTreeView
    {
        void SetupDragAndDrop()
        {
            reorderable = true;

            canStartDrag += CanStartDrag;
            setupDragAndDrop += SetupDragAndDrop;
            dragAndDropUpdate += DragAndDropUpdate;
            handleDrop += HandleDrop;
        }

        void SetupDragAndDropOnAttach()
        {
            DragAndDrop.AddDropHandler(SceneDropHandler);
        }

        void ShutdownDragAndDropOnDetach()
        {
            DragAndDrop.RemoveDropHandler(SceneDropHandler);
        }
        
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
        bool CanStartDrag(CanStartDragArgs args)
        {
            foreach (var item in selectedItems.Cast<ItemData>())
            {
                // Cannot re-order the default group
                if (item is { Group: { IsDefaultGroup: true } })
                    return false;
            }

            return true;
        }
        
        StartDragArgs SetupDragAndDrop(SetupDragAndDropArgs args)
        {
            DragAndDrop.PrepareStartDrag();

            var selection = args.selectedIds.Select(GetItemDataForId<ItemData>).Where(x => x != null);

            var genericData = selection.Select(new Func<ItemData, object>(x =>
            {
                if (x.Group is { } group)
                    return group;
                else if (x.Parameter is { } parameter)
                    return parameter;
                else
                    throw new NotSupportedException();
            }));

            var startArgs = new StartDragArgs(Contents.DragTitle, DragVisualMode.Move);
            startArgs.SetGenericData(Contents.DragDataKey, genericData.ToArray());
            return startArgs;
        }
        
        /// <summary>
        /// Receives drag & drop objects, which may originate from any Unity window, while they're being dragged.
        /// </summary>
        DragVisualMode DragAndDropUpdate(HandleDragAndDropArgs args)
        {
            if (DragAndDrop.objectReferences.Length == 0)
                return DragVisualMode.Rejected;
            
            switch (args.dropPosition)
            {
                case DragAndDropPosition.OverItem:
                    var parentItem = GetItemDataForId<ItemData>(args.parentId);
                    var rejectIfMany = DragAndDrop.objectReferences.Length > 1
                        ? DragVisualMode.Rejected
                        : DragVisualMode.Move;
                    
                    return parentItem switch
                    {
                        { IsGroup: true } => DragVisualMode.Copy,
                        { IsParameter: true } => rejectIfMany,
                        _ => throw new NotSupportedException()
                    };
            
                case DragAndDropPosition.BetweenItems:
                    return DragVisualMode.Copy;
            
                case DragAndDropPosition.OutsideItems:
                    return DragVisualMode.Copy;
            
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        /// <summary>
        /// Receives drag & drop objects, which may originate from any Unity window, after they have been dropped.
        /// </summary>
        DragVisualMode HandleDrop(HandleDragAndDropArgs args)
        {
            if (DragAndDrop.objectReferences.Length == 0)
                return DragVisualMode.Rejected;
            
            int[] newSelection;

            DragVisualMode mode = args.dropPosition switch
            {
                DragAndDropPosition.OverItem => DragAndDropOntoItem(args, out newSelection),
                DragAndDropPosition.BetweenItems => DragAndDropBetweenItems(args, out newSelection),
                DragAndDropPosition.OutsideItems => DragAndDropOutsideItems(args, out newSelection),
                _ => throw new ArgumentOutOfRangeException()
            };

            if (newSelection is { Length: > 0 })
            {
                RegisterSelectionUndoRedo();
                SelectRevealAndFrame(newSelection);
            }
            
            return mode;
        }

        DragVisualMode DragAndDropOntoItem(HandleDragAndDropArgs args, out int[] newSelection)
        {
            var parentItem = GetItemDataForId<ItemData>(args.parentId);
            
            if (parentItem.Group is { } group)
            {
                RegisterUndo(Contents.UndoDragAndDropAssignParameters);

                newSelection = new int[DragAndDrop.objectReferences.Length];
                for (var i = 0; i < DragAndDrop.objectReferences.Length; i++)
                {
                    var obj = DragAndDrop.objectReferences[i];
                    newSelection[i] = AddGenericObjectToGroup(group, obj).ID;
                }

                return DragVisualMode.Copy;
            }
            else if (parentItem.Parameter is { } parameter)
            {
                if (DragAndDrop.objectReferences.Length == 1)
                {
                    RegisterUndo(Contents.UndoDragAndDropAssignParameters);
                    
                    var firstObject = DragAndDrop.objectReferences[0];
                    AssignGenericObjectToParameter(parameter, firstObject);
                    newSelection = new[] { parameter.ID };
                    
                    var index = viewController.GetIndexForId(parameter.ID);
                    RefreshItem(index);
                    
                    return DragVisualMode.Move;
                }
            }
            else
            {
                throw new NotImplementedException();
            }

            newSelection = default;
            return DragVisualMode.Move;
        }

        DragVisualMode DragAndDropBetweenItems(HandleDragAndDropArgs args, out int[] newSelection)
        {
            var parentItem = GetItemDataForId<ItemData>(args.parentId);
            
            if (parentItem == null)
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

                m_ParameterList.m_Groups.Insert(args.insertAtIndex, newGroup);
            }
            else if (parentItem.Group is { } group)
            {
                RegisterUndo(Contents.UndoDragAndDropNewParameters);
                
                var insertAtIndex = args.childIndex;

                newSelection = new int[DragAndDrop.objectReferences.Length];
                for (var i = 0; i < DragAndDrop.objectReferences.Length; i++)
                {
                    var obj = DragAndDrop.objectReferences[i];
                    newSelection[i] = AddGenericObjectToGroup(group, obj, insertAtIndex++).ID;
                }
            }
            else
            {
                throw new NotImplementedException();
            }

            return DragVisualMode.Copy;
        }

        DragVisualMode DragAndDropOutsideItems(HandleDragAndDropArgs args, out int[] newSelection)
        {
            RegisterUndo(Contents.UndoDragAndDropNewParameters);

            var newGroup = new ParameterGroup
            {
                Name = GetUniqueGroupName(Contents.NewGroupName),
                ID = m_ParameterList.ReserveID()
            };
            
            AddGroupToTree(newGroup);

            newSelection = new int[DragAndDrop.objectReferences.Length];
            for (var i = 0; i < DragAndDrop.objectReferences.Length; i++)
            {
                var obj = DragAndDrop.objectReferences[i];
                newSelection[i] = AddGenericObjectToGroup(newGroup, obj).ID;
            }

            m_ParameterList.m_Groups.Add(newGroup);

            return DragVisualMode.Copy;
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
            
            AddParameterToTree(newParameter, group, insertAtIndex);

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
