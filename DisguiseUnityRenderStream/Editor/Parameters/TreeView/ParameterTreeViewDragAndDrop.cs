using System;
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
            
            RegisterCallback<DragExitedEvent>(OnDragExited);
        }

        void ShutdownDragAndDropOnDetach()
        {
            DragAndDrop.RemoveDropHandler(SceneDropHandler);
            
            UnregisterCallback<DragExitedEvent>(OnDragExited);
        }
        
        /// <summary>
        /// Implements drag & dropping a parameter into the scene view to select its GameObject and frame it.
        /// </summary>
        static DragAndDropVisualMode SceneDropHandler(UnityEngine.Object dropUpon, Vector3 worldPosition, Vector2 viewportPosition, Transform parentForDraggedObjects, bool perform)
        {
            var data = DragAndDrop.GetGenericData(Contents.DragDataKey);
            
            if (data is ItemData[] { Length: 1 } dataList &&
                dataList[0].Parameter is { Object: GameObject gameObject })
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
        /// Receives drag & drop objects, which may originate from any Unity window, while they're being dragged.
        /// </summary>
        DragVisualMode DragAndDropUpdate(HandleDragAndDropArgs args)
        {
            if (DragAndDrop.objectReferences.Length > 0)
            {
                return DragAndDropUpdateExternal(args);
            }
            else if (DragAndDrop.GetGenericData(Contents.DragDataKey) is ItemData[] dataList)
            {
                return DragAndDropUpdateReorder(args, dataList);
            }
            else
            {
                return DragVisualMode.Rejected;
            }
        }

        DragVisualMode DragAndDropUpdateExternal(HandleDragAndDropArgs args)
        {
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
            int[] newSelection;
            DragVisualMode mode;

            if (DragAndDrop.objectReferences.Length > 0)
            {
                mode = HandleDropExternal(args, out newSelection);
            }
            else if (DragAndDrop.GetGenericData(Contents.DragDataKey) is ItemData[] dataList)
            {
                return HandleDropReorder(args, dataList, out newSelection);
            }
            else
            {
                return DragVisualMode.Rejected;
            }

            if (newSelection is { Length: > 0 })
            {
                RegisterSelectionUndoRedo();
                SelectRevealAndFrame(newSelection);
            }
            
            return mode;
        }
        
        DragVisualMode HandleDropExternal(HandleDragAndDropArgs args, out int[] newSelection)
        {
            DragVisualMode mode = args.dropPosition switch
            {
                DragAndDropPosition.OverItem => DragAndDropOntoItem(args, out newSelection),
                DragAndDropPosition.BetweenItems => DragAndDropBetweenItems(args, out newSelection),
                DragAndDropPosition.OutsideItems => DragAndDropOutsideItems(args, out newSelection),
                _ => throw new ArgumentOutOfRangeException()
            };
            
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
                    
                    RefreshItemById(parameter.ID);
                    
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
