﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Numerics;
using System.Drawing;
using SoulsFormats;
using ImGuiNET;
using System.Net.Http.Headers;
using System.Security;
using StudioCore.Editor;
using FSParam;

namespace StudioCore.MsbEditor
{
    public class PropertyEditor
    {
        public ActionManager ContextActionManager;

        private Dictionary<string, PropertyInfo[]> _propCache = new Dictionary<string, PropertyInfo[]>();

        private object _changingObject = null;
        private object _changingPropery = null;
        private Action _lastUncommittedAction = null;

        public PropertyEditor(ActionManager manager)
        {
            ContextActionManager = manager;
        }

        private (bool, bool) PropertyRow(Type typ, object oldval, out object newval, PropertyInfo prop)
        {
            ImGui.SetNextItemWidth(-1);

            newval = null;
            bool isChanged = false;
            if (typ == typeof(long))
            {
                long val = (long)oldval;
                string strval = $@"{val}";
                if (ImGui.InputText("##value", ref strval, 99))
                {
                    var res = long.TryParse(strval, out val);
                    if (res)
                    {
                        newval = val;
                        isChanged = true;
                    }
                }
            }
            else if (typ == typeof(int))
            {
                int val = (int)oldval;
                if (ImGui.InputInt("##value", ref val))
                {
                    newval = val;
                    isChanged = true;
                }
            }
            else if (typ == typeof(uint))
            {
                uint val = (uint)oldval;
                string strval = $@"{val}";
                if (ImGui.InputText("##value", ref strval, 16))
                {
                    var res = uint.TryParse(strval, out val);
                    if (res)
                    {
                        newval = val;
                        isChanged = true;
                    }
                }
            }
            else if (typ == typeof(short))
            {
                int val = (short)oldval;
                if (ImGui.InputInt("##value", ref val))
                {
                    newval = (short)val;
                    isChanged = true;
                }
            }
            else if (typ == typeof(ushort))
            {
                ushort val = (ushort)oldval;
                string strval = $@"{val}";
                if (ImGui.InputText("##value", ref strval, 5))
                {
                    var res = ushort.TryParse(strval, out val);
                    if (res)
                    {
                        newval = val;
                        isChanged = true;
                    }
                }
            }
            else if (typ == typeof(sbyte))
            {
                int val = (sbyte)oldval;
                if (ImGui.InputInt("##value", ref val))
                {
                    newval = (sbyte)val;
                    isChanged = true;
                }
            }
            else if (typ == typeof(byte))
            {
                byte val = (byte)oldval;
                string strval = $@"{val}";
                if (ImGui.InputText("##value", ref strval, 3))
                {
                    var res = byte.TryParse(strval, out val);
                    if (res)
                    {
                        newval = val;
                        isChanged = true;
                    }
                }
                /*
                // TODO: Set Next Unique Value
                // (needs prop search to scan through structs)
                if (obj != null && ImGui.BeginPopupContextItem(propname))
                {
                    if (ImGui.Selectable("Set Next Unique Value"))
                    {
                        newval = obj.Container.GetNextUnique(propname, val);
                        _forceCommit = true;
                        ImGui.EndPopup();
                        edited = true;
                    }
                    ImGui.EndPopup();
                }
                */
            }
            else if (typ == typeof(bool))
            {
                bool val = (bool)oldval;
                if (ImGui.Checkbox("##value", ref val))
                {
                    newval = val;
                    isChanged = true;
                }
            }
            else if (typ == typeof(float))
            {
                float val = (float)oldval;
                if (ImGui.DragFloat("##value", ref val, 0.1f))
                {
                    newval = val;
                    isChanged = true;
                }
            }
            else if (typ == typeof(string))
            {
                string val = (string)oldval;
                if (val == null)
                {
                    val = "";
                }
                if (ImGui.InputText("##value", ref val, 99))
                {
                    newval = val;
                    isChanged = true;
                }
            }
            else if (typ == typeof(Vector2))
            {
                Vector2 val = (Vector2)oldval;
                if (ImGui.DragFloat2("##value", ref val, 0.1f))
                {
                    newval = val;
                    isChanged = true;
                }
            }
            else if (typ == typeof(Vector3))
            {
                Vector3 val = (Vector3)oldval;
                if (ImGui.DragFloat3("##value", ref val, 0.1f))
                {
                    newval = val;
                    isChanged = true;
                }
            }
            else if (typ.BaseType == typeof(Enum))
            {
                var enumVals = typ.GetEnumValues();
                var enumNames = typ.GetEnumNames();
                int[] intVals = new int[enumVals.Length];

                if (typ.GetEnumUnderlyingType() == typeof(byte))
                {
                    for (var i = 0; i < enumVals.Length; i++)
                        intVals[i] = (byte)enumVals.GetValue(i);

                    if (EnumEditor(enumVals, enumNames, oldval, out object val, intVals))
                    {
                        newval = val;
                        isChanged = true;
                    }
                }
                else if (typ.GetEnumUnderlyingType() == typeof(int))
                {
                    for (var i = 0; i < enumVals.Length; i++)
                        intVals[i] = (int)enumVals.GetValue(i);

                    if (EnumEditor(enumVals, enumNames, oldval, out object val, intVals))
                    {
                        newval = val;
                        isChanged = true;
                    }
                }
                else if (typ.GetEnumUnderlyingType() == typeof(uint))
                {
                    for (var i = 0; i < enumVals.Length; i++)
                        intVals[i] = (int)(uint)enumVals.GetValue(i);

                    if (EnumEditor(enumVals, enumNames, oldval, out object val, intVals))
                    {
                        newval = val;
                        isChanged = true;
                    }
                }
                else
                {
                    ImGui.Text("ImplementMe");
                }
            }
            else if (typ == typeof(Color))
            {
                var att = prop?.GetCustomAttribute<SupportsAlphaAttribute>();
                if (att != null)
                {
                    if (att.Supports == false)
                    {
                        var color = (Color)oldval;
                        Vector3 val = new(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f);
                        if (ImGui.ColorEdit3("##value", ref val))
                        {
                            Color newColor = Color.FromArgb((int)(val.X * 255.0f), (int)(val.Y * 255.0f), (int)(val.Z * 255.0f));
                            newval = newColor;
                            isChanged = true;
                        }
                    }
                    else
                    {
                        var color = (Color)oldval;
                        Vector4 val = new(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f);
                        if (ImGui.ColorEdit4("##value", ref val))
                        {
                            Color newColor = Color.FromArgb((int)(val.W * 255.0f), (int)(val.X * 255.0f), (int)(val.Y * 255.0f), (int)(val.Z * 255.0f));
                            newval = newColor;
                            isChanged = true;
                        }
                    }
                }
                else
                {
                    // SoulsFormats does not define if alpha should be exposed. Expose alpha by default.
                    TaskLogs.AddLog($"Color property in \"{prop.DeclaringType}\" does not declare if it supports Alpha. Alpha will be exposed by default",
                        Microsoft.Extensions.Logging.LogLevel.Warning, TaskLogs.LogPriority.Low);

                    var color = (Color)oldval;
                    Vector4 val = new(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f);
                    if (ImGui.ColorEdit4("##value", ref val))
                    {
                        Color newColor = Color.FromArgb((int)(val.W * 255.0f), (int)(val.X * 255.0f), (int)(val.Y * 255.0f), (int)(val.Z * 255.0f));
                        newval = newColor;
                        isChanged = true;
                    }
                }
            }
            else
            {
                ImGui.Text("ImplementMe");
            }
            bool isDeactivatedAfterEdit = ImGui.IsItemDeactivatedAfterEdit() || !ImGui.IsAnyItemActive();

            // Tooltip
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.DelayNormal | ImGuiHoveredFlags.NoSharedDelay))
            {
                string str = $"Value Type: {typ.Name}";
                if (typ.IsValueType)
                {
                    var min = typ.GetField("MinValue")?.GetValue(typ);
                    var max = typ.GetField("MaxValue")?.GetValue(typ);
                    if (min != null & max != null)
                    {
                        str += $" (Min {min}, Max {max})";
                    }
                }
                ImGui.SetTooltip(str);
            }

            return (isChanged, isDeactivatedAfterEdit);
        }


        private bool EnumEditor(Array enumVals, string[] enumNames, object oldval, out object val, int[] intVals)
        {
            val = null;

            for (var i = 0; i < enumNames.Length; i++)
            {
                enumNames[i] = $"{intVals[i]}: {enumNames[i]}";
            }

            int index = Array.IndexOf(enumVals, oldval);
            if (ImGui.Combo("", ref index, enumNames, enumNames.Length))
            {
                val = enumVals.GetValue(index);
                return true;
            }

            return false;
        }

        private void UpdateProperty(object prop, Entity selection, object obj, object newval,
            bool changed, bool committed, int arrayindex = -1)
        {
            // TODO2: strip this out in favor of other ChangeProperty
            if (changed)
            {
                ChangeProperty(prop, selection, obj, newval, ref committed, arrayindex);
            }
            if (committed)
            {
                CommitProperty(selection, false);
            }
        }

        private void ChangeProperty(object prop, Entity selection, object obj, object newval,
            ref bool committed, int arrayindex = -1)
        {
            if (prop == _changingPropery && _lastUncommittedAction != null && ContextActionManager.PeekUndoAction() == _lastUncommittedAction)
            {
                ContextActionManager.UndoAction();
            }
            else
            {
                _lastUncommittedAction = null;
            }

            if (_changingObject != null && selection != null && selection.WrappedObject != _changingObject)
            {
                committed = true;
            }
            else
            {
                PropertiesChangedAction action;
                if (arrayindex != -1)
                {
                    action = new PropertiesChangedAction((PropertyInfo)prop, arrayindex, obj, newval);
                }
                else
                {
                    action = new PropertiesChangedAction((PropertyInfo)prop, obj, newval);
                }
                ContextActionManager.ExecuteAction(action);

                _lastUncommittedAction = action;
                _changingPropery = prop;
                _changingObject = selection != null ? selection.WrappedObject : obj;
            }
        }
        private void CommitProperty(Entity selection, bool destroyRenderModel)
        {
            // Invalidate name cache
            if (selection != null)
            {
                selection.Name = null;
            }

            // Undo and redo the last action with a rendering update
            if (_lastUncommittedAction != null && ContextActionManager.PeekUndoAction() == _lastUncommittedAction)
            {
                if (_lastUncommittedAction is PropertiesChangedAction a)
                {
                    // Kinda a hack to prevent a jumping glitch
                    a.SetPostExecutionAction(null);
                    ContextActionManager.UndoAction();
                    if (selection != null)
                    {
                        a.SetPostExecutionAction((undo) =>
                        {
                            if (destroyRenderModel)
                            {
                                if (selection.RenderSceneMesh != null)
                                {
                                    selection.RenderSceneMesh.Dispose();
                                    selection.RenderSceneMesh = null;
                                }
                            }
                            selection.UpdateRenderModel();
                        });
                    }
                    ContextActionManager.ExecuteAction(a);
                }
            }

            _lastUncommittedAction = null;
            _changingPropery = null;
            _changingObject = null;
        }


        /// <summary>
        /// Handles changing a property's value for any number of entities.
        /// </summary>
        /// <param name="arrayindex">Index of the targeted value in an array of values.</param>
        /// <param name="classIndex">Index of the targeted class in an array of classes.</param>
        private void UpdateProperty(object prop, HashSet<Entity> selection, object newval,
            bool changed, bool committed, int arrayindex, int classIndex)
        {
            if (changed)
            {
                ChangePropertyMultiple(prop, selection, newval, ref committed, arrayindex, classIndex);
            }
            if (committed)
            {
                if (_lastUncommittedAction != null && ContextActionManager.PeekUndoAction() == _lastUncommittedAction)
                {
                    if (_lastUncommittedAction is MultipleEntityPropertyChangeAction a)
                    {
                        ContextActionManager.UndoAction();
                        a.UpdateRenderModel = true; // Update render model on commit execution, and update on undo/redo.
                        ContextActionManager.ExecuteAction(a);
                    }
                    _lastUncommittedAction = null;
                    _changingPropery = null;
                    _changingObject = null;
                }
            }
        }

        /// <summary>
        /// Change a property's value for any number of entities.
        /// </summary>
        /// <param name="arrayindex">Index of the targeted value in an array of values.</param>
        /// <param name="classIndex">Index of the targeted class in an array of classes.</param>
        private void ChangePropertyMultiple(object prop, HashSet<Entity> ents, object newval, ref bool committed, int arrayindex = -1, int classIndex = -1)
        {
            if (prop == _changingPropery && _lastUncommittedAction != null && ContextActionManager.PeekUndoAction() == _lastUncommittedAction)
            {
                ContextActionManager.UndoAction();
            }
            else
            {
                _lastUncommittedAction = null;
            }
            MultipleEntityPropertyChangeAction action;
            foreach (var selection in ents)
            {
                if (selection != null && _changingObject != null && !ents.SetEquals((HashSet<Entity>)_changingObject))
                {
                    committed = true;
                    return;
                }

            }
            action = new MultipleEntityPropertyChangeAction((PropertyInfo)prop, ents, newval, arrayindex, classIndex);
            ContextActionManager.ExecuteAction(action);

            _lastUncommittedAction = action;
            _changingPropery = prop;
            _changingObject = ents;
        }

        private void PropEditorParamRow(Entity selection)
        {
            IReadOnlyList<Param.Cell> cells = new List<Param.Cell>();
            if (selection.WrappedObject is Param.Row row)
            {
                cells = row.Cells;

            }
            else if (selection.WrappedObject is MergedParamRow mrow)
            {
                cells = mrow.CellHandles;
            }
            ImGui.Columns(2);
            ImGui.Separator();
            int id = 0;

            // This should be rewritten somehow it's super ugly
            var nameProp = selection.WrappedObject.GetType().GetProperty("Name");
            var idProp = selection.WrappedObject.GetType().GetProperty("ID");
            PropEditorPropInfoRow(selection.WrappedObject, nameProp, "Name", ref id, selection);
            PropEditorPropInfoRow(selection.WrappedObject, idProp, "ID", ref id, selection);

            foreach (var cell in cells)
            {
                PropEditorPropCellRow(cell, ref id, selection);
            }
            ImGui.Columns(1);
        }

        public void PropEditorParamRow(Param.Row row)
        {
            ImGui.Columns(2);
            ImGui.Separator();
            int id = 0;

            // This should be rewritten somehow it's super ugly
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.8f, 0.8f, 0.8f, 1.0f));
            var nameProp = row.GetType().GetProperty("Name");
            var idProp = row.GetType().GetProperty("ID");
            PropEditorPropInfoRow(row, nameProp, "Name", ref id, null);
            PropEditorPropInfoRow(row, idProp, "ID", ref id, null);
            ImGui.PopStyleColor();

            foreach (var cell in row.Columns)
            {
                PropEditorPropCellRow(row[cell], ref id, null);
            }
            ImGui.Columns(1);
        }

        // Many parameter options, which may be simplified.
        private void PropEditorPropInfoRow(object rowOrWrappedObject, PropertyInfo prop, string visualName, ref int id, Entity nullableSelection)
        {
            PropEditorPropRow(prop.GetValue(rowOrWrappedObject), ref id, visualName, prop.PropertyType, null, null, prop, rowOrWrappedObject, nullableSelection);
        }
        private void PropEditorPropCellRow(Param.Cell cell, ref int id, Entity nullableSelection)
        {
            PropEditorPropRow(cell.Value, ref id, cell.Def.InternalName, cell.Value.GetType(), null, cell.Def.InternalName, cell.GetType().GetProperty("Value"), cell, nullableSelection);
        }
        private void PropEditorPropRow(object oldval, ref int id, string visualName, Type propType, Entity nullableEntity, string nullableName, PropertyInfo proprow, object paramRowOrCell, Entity nullableSelection)
        {
            ImGui.PushID(id);
            ImGui.AlignTextToFramePadding();
            ImGui.Text(visualName);
            ImGui.NextColumn();
            ImGui.SetNextItemWidth(-1);

            object newval;
            // Property Editor UI
            (bool, bool) propEditResults = PropertyRow(propType, oldval, out newval, proprow);
            bool changed = propEditResults.Item1;
            bool committed = propEditResults.Item2;
            UpdateProperty(proprow, nullableSelection, paramRowOrCell, newval, changed, committed);
            ImGui.NextColumn();
            ImGui.PopID();
            id++;
        }


        private void PropertyContextMenu(object obj, PropertyInfo propinfo)
        {
            if (ImGui.BeginPopupContextItem(propinfo.Name))
            {
                if (ImGui.Selectable($@"Search"))
                {
                    EditorCommandQueue.AddCommand($@"map/propsearch/{propinfo.Name}");
                }
                ImGui.EndPopup();
            }
        }
        private bool ParamRefRow(PropertyInfo propinfo, object val, ref object newObj)
        {
            List<MSBParamReference> attributes = propinfo.GetCustomAttributes<MSBParamReference>().ToList();
            if (attributes.Any())
            {
                List<ParamEditor.ParamRef> refs = new List<ParamEditor.ParamRef>();
                foreach (var att in attributes)
                {
                    refs.Add(new ParamEditor.ParamRef(att.ParamName));
                }
                ImGui.NextColumn();
                    
                Editor.EditorDecorations.ParamRefText(refs, null);
                ImGui.NextColumn();
                Editor.EditorDecorations.ParamRefsSelectables(ParamEditor.ParamBank.PrimaryBank, refs, null, val);
                var opened = Editor.EditorDecorations.ParamRefEnumContextMenu(ParamEditor.ParamBank.PrimaryBank, val, ref newObj, refs, null, null, null, null);
                return opened;
            }
            return false;
        }

        private void PropEditorFlverLayout(Entity selection, FLVER2.BufferLayout layout)
        {
            foreach (var l in layout)
            {
                ImGui.Text(l.Semantic.ToString());
                ImGui.NextColumn();
                ImGui.Text(l.Type.ToString());
                ImGui.NextColumn();
            }
        }

        internal enum RegionShape
        {
            Point,
            Sphere,
            Cylinder,
            Box,
            Composite,
            Rectangle,
            Circle,
        }

        private string[] _regionShapes =
        {
            "Point",
            "Sphere",
            "Cylinder",
            "Box",
            "Composite",
            "Rectangle",
            "Circle",
        };

        internal enum LightType
        {
            Spot,
            Directional,
            Point,
        }

        private string[] _lightTypes =
        {
            "Spot",
            "Directional",
            "Point",
        };

        private void PropEditorGeneric(Selection selection, HashSet<Entity> entSelection, object target = null, bool decorate = true, int classIndex = -1)
        {
            float scale = MapStudioNew.GetUIScale();
            var firstEnt = entSelection.First();
            var obj = (target == null) ? firstEnt.WrappedObject : target;
            var type = obj.GetType();
            if (!_propCache.ContainsKey(type.FullName))
            {
                var props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
                props = props.OrderBy(p => p.MetadataToken).ToArray();
                _propCache.Add(type.FullName, props);
            }
            var properties = _propCache[type.FullName];

            if (decorate)
            {
                ImGui.Columns(2);
                ImGui.Separator();
                ImGui.Text("Object Type");
                ImGui.NextColumn();
                ImGui.Text(type.Name);
                ImGui.NextColumn();
            }

            // Custom editors
            if (type == typeof(FLVER2.BufferLayout))
            {
                if (entSelection.Count() == 1)
                    PropEditorFlverLayout(firstEnt, (FLVER2.BufferLayout)obj);
                else
                    ImGui.Text("Cannot edit multiples of this object at once.");
            }
            else
            {
                int id = 0;
                foreach (var prop in properties)
                {
                    if (!prop.CanWrite && !prop.PropertyType.IsArray)
                    {
                        continue;
                    }

                    if (prop.GetCustomAttribute<HideProperty>() != null)
                    {
                        continue;
                    }

                    ImGui.PushID(id);
                    ImGui.AlignTextToFramePadding();
                    var typ = prop.PropertyType;

                    if (typ.IsArray)
                    {
                        Array a = (Array)prop.GetValue(obj);
                        for (int i = 0; i < a.Length; i++)
                        {
                            ImGui.PushID(i);

                            var arrtyp = typ.GetElementType();
                            if (arrtyp.IsClass && arrtyp != typeof(string) && !arrtyp.IsArray)
                            {
                                bool open = ImGui.TreeNodeEx($@"{prop.Name}[{i}]", ImGuiTreeNodeFlags.DefaultOpen);
                                ImGui.NextColumn();
                                ImGui.SetNextItemWidth(-1);
                                var o = a.GetValue(i);
                                ImGui.Text(o.GetType().Name);
                                ImGui.NextColumn();
                                if (open)
                                {
                                    PropEditorGeneric(selection, entSelection, o, false, i);
                                    ImGui.TreePop();
                                }
                                ImGui.PopID();
                            }
                            else
                            {
                                ImGui.Text($@"{prop.Name}[{i}]");
                                ImGui.NextColumn();
                                ImGui.SetNextItemWidth(-1);
                                var oldval = a.GetValue(i);
                                object newval = null;

                                // Property Editor UI
                                (bool, bool) propEditResults = PropertyRow(typ.GetElementType(), oldval, out newval, prop);
                                bool changed = propEditResults.Item1;
                                bool committed = propEditResults.Item2;
                                PropertyContextMenu(obj, prop);
                                if (ImGui.IsItemActive() && !ImGui.IsWindowFocused())
                                {
                                    ImGui.SetItemDefaultFocus();
                                }
                                if (ParamRefRow(prop, oldval, ref newval))
                                {
                                    changed = true;
                                    committed = true;
                                }
                                UpdateProperty(prop, entSelection, newval, changed, committed, i, classIndex);

                                ImGui.NextColumn();
                                ImGui.PopID();
                            }
                        }
                        ImGui.PopID();
                    }
                    else if (typ.IsGenericType && typ.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        object l = prop.GetValue(obj);
                        PropertyInfo itemprop = l.GetType().GetProperty("Item");
                        int count = (int)l.GetType().GetProperty("Count").GetValue(l);
                        for (int i = 0; i < count; i++)
                        {
                            ImGui.PushID(i);

                            var arrtyp = typ.GetGenericArguments()[0];
                            if (arrtyp.IsClass && arrtyp != typeof(string) && !arrtyp.IsArray)
                            {
                                bool open = ImGui.TreeNodeEx($@"{prop.Name}[{i}]", ImGuiTreeNodeFlags.DefaultOpen);
                                ImGui.NextColumn();
                                ImGui.SetNextItemWidth(-1);
                                var o = itemprop.GetValue(l, new object[] { i });
                                ImGui.Text(o.GetType().Name);
                                ImGui.NextColumn();
                                if (open)
                                {
                                    PropEditorGeneric(selection, entSelection, o, false);
                                    ImGui.TreePop();
                                }
                                ImGui.PopID();
                            }
                            else
                            {
                                ImGui.Text($@"{prop.Name}[{i}]");
                                ImGui.NextColumn();
                                ImGui.SetNextItemWidth(-1);
                                var oldval = itemprop.GetValue(l, new object[] { i });
                                object newval = null;

                                // Property Editor UI
                                (bool, bool) propEditResults = PropertyRow(arrtyp, oldval, out newval, prop);
                                bool changed = propEditResults.Item1;
                                bool committed = propEditResults.Item2;
                                PropertyContextMenu(obj, prop);
                                if (ImGui.IsItemActive() && !ImGui.IsWindowFocused())
                                {
                                    ImGui.SetItemDefaultFocus();
                                }
                                if (ParamRefRow(prop, oldval, ref newval))
                                {
                                    changed = true;
                                    committed = true;
                                }
                                UpdateProperty(prop, entSelection, newval, changed, committed, i, classIndex);

                                ImGui.NextColumn();
                                ImGui.PopID();
                            }
                        }
                        ImGui.PopID();
                    }
                    else if (typ.IsClass && typ == typeof(MSB.Shape))
                    {
                        bool open = ImGui.TreeNodeEx(prop.Name, ImGuiTreeNodeFlags.DefaultOpen);
                        ImGui.NextColumn();
                        ImGui.SetNextItemWidth(-1);
                        var o = prop.GetValue(obj);
                        var shapetype = Enum.Parse<RegionShape>(o.GetType().Name);
                        int shap = (int)shapetype;

                        if (entSelection.Count == 1)
                        {
                            if (ImGui.Combo("##shapecombo", ref shap, _regionShapes, _regionShapes.Length))
                            {
                                MSB.Shape newshape;
                                switch ((RegionShape)shap)
                                {
                                    case RegionShape.Box:
                                        newshape = new MSB.Shape.Box();
                                        break;
                                    case RegionShape.Point:
                                        newshape = new MSB.Shape.Point();
                                        break;
                                    case RegionShape.Cylinder:
                                        newshape = new MSB.Shape.Cylinder();
                                        break;
                                    case RegionShape.Sphere:
                                        newshape = new MSB.Shape.Sphere();
                                        break;
                                    case RegionShape.Composite:
                                        newshape = new MSB.Shape.Composite();
                                        break;
                                    case RegionShape.Rectangle:
                                        newshape = new MSB.Shape.Rectangle();
                                        break;
                                    case RegionShape.Circle:
                                        newshape = new MSB.Shape.Circle();
                                        break;
                                    default:
                                        throw new Exception("Invalid shape");
                                }
                                var action = new PropertiesChangedAction((PropertyInfo)prop, obj, newshape);
                                action.SetPostExecutionAction((undo) =>
                                {
                                    bool selected = false;
                                    if (firstEnt.RenderSceneMesh != null)
                                    {
                                        selected = firstEnt.RenderSceneMesh.RenderSelectionOutline;
                                        firstEnt.RenderSceneMesh.Dispose();
                                        firstEnt.RenderSceneMesh = null;
                                    }

                                    firstEnt.UpdateRenderModel();
                                    firstEnt.RenderSceneMesh.RenderSelectionOutline = selected;
                                });
                                ContextActionManager.ExecuteAction(action);
                            }
                        }
                        ImGui.NextColumn();
                        if (open)
                        {
                            PropEditorGeneric(selection, entSelection, o, false);
                            ImGui.TreePop();
                        }
                        ImGui.PopID();
                    }
                    else if (typ == typeof(BTL.LightType))
                    {
                        bool open = ImGui.TreeNodeEx(prop.Name, ImGuiTreeNodeFlags.DefaultOpen);
                        ImGui.NextColumn();
                        ImGui.SetNextItemWidth(-1);
                        var o = prop.GetValue(obj);
                        var enumTypes = Enum.Parse<LightType>(o.ToString());
                        int thisType = (int)enumTypes;
                        if (ImGui.Combo("##lightTypecombo", ref thisType, _lightTypes, _lightTypes.Length))
                        {
                            BTL.LightType newLight;
                            switch ((LightType)thisType)
                            {
                                case LightType.Directional:
                                    newLight = BTL.LightType.Directional;
                                    break;
                                case LightType.Point:
                                    newLight = BTL.LightType.Point;
                                    break;
                                case LightType.Spot:
                                    newLight = BTL.LightType.Spot;
                                    break;
                                default:
                                    throw new Exception("Invalid BTL LightType");
                            }
                            var action = new PropertiesChangedAction((PropertyInfo)prop, obj, newLight);
                            action.SetPostExecutionAction((undo) =>
                            {
                                bool selected = false;
                                if (firstEnt.RenderSceneMesh != null)
                                {
                                    selected = firstEnt.RenderSceneMesh.RenderSelectionOutline;
                                    firstEnt.RenderSceneMesh.Dispose();
                                    firstEnt.RenderSceneMesh = null;
                                }

                                firstEnt.UpdateRenderModel();
                                firstEnt.RenderSceneMesh.RenderSelectionOutline = selected;
                            });
                            ContextActionManager.ExecuteAction(action);

                            ContextActionManager.ExecuteAction(action);
                        }
                        ImGui.NextColumn();
                        if (open)
                        {
                            PropEditorGeneric(selection, entSelection, o, false);
                            ImGui.TreePop();
                        }
                        ImGui.PopID();
                    }
                    else if (typ.IsClass && typ != typeof(string) && !typ.IsArray)
                    {
                        bool open = ImGui.TreeNodeEx(prop.Name, ImGuiTreeNodeFlags.DefaultOpen);
                        ImGui.NextColumn();
                        ImGui.SetNextItemWidth(-1);
                        var o = prop.GetValue(obj);
                        ImGui.Text(o.GetType().Name);
                        ImGui.NextColumn();
                        if (open)
                        {
                            PropEditorGeneric(selection, entSelection, o, false);
                            ImGui.TreePop();
                        }
                        ImGui.PopID();
                    }
                    else
                    {
                        ImGui.Text(prop.Name);
                        ImGui.NextColumn();
                        ImGui.SetNextItemWidth(-1);
                        var oldval = prop.GetValue(obj);
                        object newval = null;

                        // Property Editor UI
                        (bool, bool) propEditResults = PropertyRow(typ, oldval, out newval, prop);
                        bool changed = propEditResults.Item1;
                        bool committed = propEditResults.Item2;
                        PropertyContextMenu(obj, prop);
                        if (ImGui.IsItemActive() && !ImGui.IsWindowFocused())
                        {
                            ImGui.SetItemDefaultFocus();
                        }
                        if (ParamRefRow(prop, oldval, ref newval))
                        {
                            changed = true;
                            committed = true;
                        }
                        UpdateProperty(prop, entSelection, newval, changed, committed, -1, classIndex);

                        ImGui.NextColumn();
                        ImGui.PopID();
                    }
                    id++;
                }
            }

            var refID = 0; // ID for ImGui distinction
            if (decorate && entSelection.Count == 1)
            {
                ImGui.Columns(1);
                if (firstEnt.References != null)
                {
                    ImGui.NewLine();
                    ImGui.Text("References: ");
                    ImGui.Indent(10 * scale);
                    foreach (var m in firstEnt.References)
                    {
                        foreach (var n in m.Value)
                        {
                            if (n is Entity e)
                            {
                                var nameWithType = e.PrettyName.Insert(2, e.WrappedObject.GetType().Name + " - ");
                                if (ImGui.Button(nameWithType + "##MSBRefTo" + refID))
                                {
                                    selection.ClearSelection();
                                    selection.AddSelection(e);
                                }
                            }
                            else if (n is ObjectContainerReference r)
                            {
                                // Try to select the map's RootObject if it is loaded, and the reference otherwise.
                                // It's not the end of the world if we choose the wrong one, as SceneTree can use either,
                                // but only the RootObject has the TransformNode and Viewport integration.
                                string mapid = r.Name;
                                string prettyName = $"{ForkAwesome.Cube} {mapid}";
                                if (Editor.AliasBank.MapNames != null && Editor.AliasBank.MapNames.TryGetValue(mapid, out string metaName))
                                {
                                    prettyName += $" <{metaName.Replace("--", "")}>";
                                }
                                if (ImGui.Button(prettyName + "##MSBRefTo" + refID))
                                {
                                    Scene.ISelectable rootTarget = r.GetSelectionTarget();
                                    selection.ClearSelection();
                                    selection.AddSelection(rootTarget);
                                    // For this type of connection, jump to the object in the list to actually load the map
                                    // (is this desirable in other cases?). It could be possible to have a Load context menu
                                    // here, but that should be shared with SceneTree.
                                    selection.GotoTreeTarget = rootTarget;
                                }

                                if (ImGui.BeginPopupContextItem())
                                {
                                    var map = firstEnt.Universe.GetLoadedMap(mapid);
                                    if (map == null)
                                    {
                                        if (ImGui.Selectable("Load Map"))
                                        {
                                            firstEnt.Universe.LoadMap(mapid);
                                        }
                                    }
                                    else
                                    {
                                        if (ImGui.Selectable("Unload Map"))
                                        {
                                            firstEnt.Universe.UnloadContainer(map);
                                        }
                                    }
                                    ImGui.EndPopup();
                                }
                            }
                            refID++;
                        }
                    }
                }
                ImGui.Unindent(10 * scale);
                ImGui.NewLine();
                ImGui.Text("Objects referencing this object:");
                ImGui.Indent(10 * scale);
                foreach (var m in firstEnt.GetReferencingObjects())
                {
                    var nameWithType = m.PrettyName.Insert(2, m.WrappedObject.GetType().Name + " - ");
                    if (ImGui.Button(nameWithType + "##MSBRefBy" + refID))
                    {
                        selection.ClearSelection();
                        selection.AddSelection(m);
                    }
                    refID++;
                }
                ImGui.Unindent(10 * scale);
            }
        }

        public void OnGui(Selection selection, string id, float w, float h)
        {
            float scale = MapStudioNew.GetUIScale();
            var entSelection = selection.GetFilteredSelection<Entity>();

            ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0.145f, 0.145f, 0.149f, 1.0f));
            ImGui.SetNextWindowSize(new Vector2(350, h - 80) * scale, ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowPos(new Vector2(w - 370, 20) * scale, ImGuiCond.FirstUseEver);
            ImGui.Begin($@"Properties##{id}");
            ImGui.BeginChild("propedit");
            if (entSelection.Count > 1)
            {
                var firstEnt = entSelection.First();
                if (entSelection.All(e => e.WrappedObject.GetType() == firstEnt.WrappedObject.GetType()))
                {
                    if (firstEnt.WrappedObject is Param.Row prow || firstEnt.WrappedObject is MergedParamRow)
                    {
                        ImGui.Text("Cannot edit multiples of this object at once.");
                        ImGui.EndChild();
                        ImGui.End();
                        ImGui.PopStyleColor();
                        return;
                    }
                    else
                    {
                        ImGui.TextColored(new Vector4(0.5f, 1.0f, 0.0f, 1.0f), " Editing Multiple Objects.\n Changes will be applied to all selected objects.");
                        ImGui.Separator();
                        ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0.0f, 0.5f, 0.0f, 0.1f));
                        ImGui.BeginChild("MSB_EditingMultipleObjsChild");
                        PropEditorGeneric(selection, entSelection);
                        ImGui.PopStyleColor();
                        ImGui.EndChild();
                    }
                }
                else
                {
                    ImGui.Text("Not all selected objects are the same type.");
                    ImGui.EndChild();
                    ImGui.End();
                    ImGui.PopStyleColor();
                    return;
                }

            }
            else if (entSelection.Any())
            {
                var firstEnt = entSelection.First();
                ImGui.Text($" Map: {firstEnt.Container.Name}");
                if (firstEnt.WrappedObject == null)
                {
                    ImGui.Text("Select a map object to edit its properties.");
                    ImGui.EndChild();
                    ImGui.End();
                    ImGui.PopStyleColor();
                    return;
                }
                else if (firstEnt.WrappedObject is Param.Row prow || firstEnt.WrappedObject is MergedParamRow)
                {
                    PropEditorParamRow(firstEnt);
                }
                else
                {
                    PropEditorGeneric(selection, entSelection);
                }
            }
            else
            {
                ImGui.Text("Select a map object to edit its properties.");
            }
            ImGui.EndChild();
            ImGui.End();
            ImGui.PopStyleColor();
        }
    }
}