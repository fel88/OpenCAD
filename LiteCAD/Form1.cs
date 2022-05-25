﻿using IxMilia.Dxf;
using IxMilia.Dxf.Entities;
using LiteCAD.BRep;
using LiteCAD.BRep.Editor;
using LiteCAD.Common;
using LiteCAD.DraftEditor;
using LiteCAD.Parsers.Step;
using LiteCAD.PropEditors;
using LiteCAD.Tools;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace LiteCAD
{
    public partial class Form1 : Form, IEditor
    {
        public static Form1 Form;
        private void Form1_Load(object sender, EventArgs e)
        {
            mf = new MessageFilter();
            Application.AddMessageFilter(mf);
        }

        MessageFilter mf = null;
        GLControl glControl;
        public EventWrapperGlControl evwrapper;
        public Camera camera1 = new Camera() { IsOrtho = true };
        public CameraViewManager ViewManager;
        private void Gl_Paint(object sender, PaintEventArgs e)
        {
            //if (!loaded)
            //  return;
            if (!glControl.Context.IsCurrent)
            {
                glControl.MakeCurrent();
            }

            Redraw();
        }

        public IntersectInfo Pick => pick;
        IntersectInfo pick;

        public void UpdatePickTriangle()
        {
            List<IMeshNodesContainer> ret = new List<IMeshNodesContainer>();
            foreach (var item in Parts)
            {
                ret.AddRange(item.GetAll(z => z is IMeshNodesContainer).OfType<IMeshNodesContainer>());
            }

            List<IntersectInfo> rr = new List<IntersectInfo>();
            foreach (var item in ret.OfType<PartInstance>())
            {
                if (item is IDrawable d)
                {
                    if (!d.Visible) continue;
                }
                var r = UpdatePickTriangle(item);
                if (r != null)
                    rr.Add(r);
            }
            pick = null;
            if (rr.Any())
            {
                pick = rr.OrderBy(z => z.Distance).First();
            }
        }

        public IntersectInfo UpdatePickTriangle(IMeshNodesContainer part)
        {
            //if (CurrentModel == null) return;
            //var r = (CurrentModel.ShellInfo as RectTubeShellInfo);

            MouseRay.UpdateMatrices();
            var mr = new MouseRay(glControl.PointToClient(Cursor.Position));

            //var mds = Helpers.CurrentModel.Nodes.Where(z=>z.IsVisible).Where(z => z.Tag is NodeInfo).Select(z => z.Model).ToArray();
            //var mds = part.Nodes.Where(z => z.IsVisible).Select(z => z.Model).ToArray();

            var inter = Intersection.AllRayIntersect(part, mr);
            if (inter == null || inter.Count() == 0) return null;
            var fr = inter.OrderBy(z => z.Distance).First();

            var rpick = fr;
            rpick.Model = part;
            return rpick;
        }

        bool pickEnable = true;

        bool drawAxes = true;
        Vector3d lastHovered;
        void Redraw()
        {
            UpdatePickTriangle();

            CurrentTool.Update();
            ViewManager.Update();

            GL.ClearColor(Color.LightGray);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.Viewport(0, 0, glControl.Width, glControl.Height);
            var o2 = Matrix4.CreateOrthographic(glControl.Width, glControl.Height, 1, 1000);

            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref o2);

            Matrix4 modelview2 = Matrix4.LookAt(0, 0, 70, 0, 0, 0, 0, 1, 0);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadMatrix(ref modelview2);

            GL.Enable(EnableCap.DepthTest);

            float zz = -500;
            GL.Begin(PrimitiveType.Quads);
            GL.Color3(Color.LightBlue);
            GL.Vertex3(-glControl.Width / 2, -glControl.Height / 2, zz);
            GL.Vertex3(glControl.Width / 2, -glControl.Height / 2, zz);
            GL.Color3(Color.AliceBlue);
            GL.Vertex3(glControl.Width / 2, glControl.Height / 2, zz);
            GL.Vertex3(-glControl.Width / 2, glControl.Height, zz);
            GL.End();

            GL.PushMatrix();
            GL.Translate(camera1.viewport[2] / 2 - 50, -camera1.viewport[3] / 2 + 50, 0);
            GL.Scale(0.5, 0.5, 0.5);

            var mtr = camera1.ViewMatrix;
            var q = mtr.ExtractRotation();
            var mtr3 = Matrix4.CreateFromQuaternion(q);
            GL.MultMatrix(ref mtr3);
            GL.LineWidth(2);
            GL.Color3(Color.Red);
            GL.Begin(PrimitiveType.Lines);
            GL.Vertex3(0, 0, 0);
            GL.Vertex3(100, 0, 0);
            GL.End();

            GL.Color3(Color.Green);
            GL.Begin(PrimitiveType.Lines);
            GL.Vertex3(0, 0, 0);
            GL.Vertex3(0, 100, 0);
            GL.End();

            GL.Color3(Color.Blue);
            GL.Begin(PrimitiveType.Lines);
            GL.Vertex3(0, 0, 0);
            GL.Vertex3(0, 0, 100);
            GL.End();
            GL.PopMatrix();
            camera1.Setup(glControl);

            if (drawAxes)
            {
                GL.PushMatrix();

                GL.LineWidth(2);
                GL.Color3(Color.Red);
                GL.Begin(PrimitiveType.Lines);
                GL.Vertex3(0, 0, 0);
                GL.Vertex3(100, 0, 0);
                GL.End();

                GL.Color3(Color.Green);
                GL.Begin(PrimitiveType.Lines);
                GL.Vertex3(0, 0, 0);
                GL.Vertex3(0, 100, 0);
                GL.End();

                GL.Color3(Color.Blue);
                GL.Begin(PrimitiveType.Lines);
                GL.Vertex3(0, 0, 0);
                GL.Vertex3(0, 0, 100);
                GL.End();
                GL.PopMatrix();
            }

            GL.Color3(Color.Blue);
            GL.Enable(EnableCap.Lighting);
            GL.Enable(EnableCap.Light0);

            GL.ShadeModel(ShadingModel.Smooth);

            IDrawable[] parts = null;
            lock (Parts)
            {
                parts = Parts.ToArray();
            }

            foreach (var item in parts)
            {
                if (!item.Visible) continue;
                item.Draw();
            }

            CurrentTool.Draw();
            if (pick != null)
            {
                float pickEps = 10;

                var pp = new[] { pick.Target.V0,
                pick.Target.V1,pick.Target.V2}.ToArray();

                if (pp.Any(uu => (uu - pick.Point).Length < pickEps))
                {
                    var fr = pp.First(uu => (uu - pick.Point).Length < pickEps);
                    lastHovered = fr;
                    toolStripStatusLabel3.Text = $"hovered point: X: {fr.X:N3} Y: {fr.Y:N3} Z: {fr.Z:N3}";
                    float gap1 = 3;
                    GL.Disable(EnableCap.Lighting);
                    //draw 3d rect
                    GL.Color3(Color.Red);

                    GL.PointSize(10);
                    GL.Begin(PrimitiveType.Points);
                    GL.Vertex3(fr);
                    GL.End();
                }
                //select plane
                else if (pick.Model is IPartContainer part && pick.Target != null)
                {
                    MeshNode frr = null;
                    if (part is PartInstance pii)
                    {
                        var mtr1 = pii.Matrix.Calc();
                        frr = part.Part.Nodes.FirstOrDefault(zzz => zzz.Contains(pick.Target, mtr1));
                    }
                    else
                    {
                        frr = part.Part.Nodes.FirstOrDefault(zzz => zzz.Contains(pick.Target));
                    }
                    if (frr != null)
                    {
                        var face = frr.Parent;
                        BRep.BRepWire wire = null;
                        if (part is PartInstance pii2)
                        {
                            wire = face.Wires.FirstOrDefault(yy => GeometryUtils.Contains(yy, pick.Target, pii2.Matrix.Calc()));
                        }
                        else
                        {
                            wire = face.Wires.FirstOrDefault(yy => GeometryUtils.Contains(yy, pick.Target));
                        }

                        if (wire != null)
                        {
                            GL.Disable(EnableCap.Lighting);
                            GL.LineWidth(3);
                            GL.PushMatrix();
                            if (part is PartInstance pii3)
                            {
                                var ref1 = pii3.Matrix.Calc();
                                GL.MultMatrix(ref ref1);

                            }
                            GL.Color3(Color.Orange);
                            GL.Begin(PrimitiveType.Lines);
                            foreach (var edge in wire.Edges)
                            {

                                GL.Vertex3(edge.Start);
                                GL.Vertex3(edge.End);
            }
                            GL.End();
                            GL.LineWidth(1);
                            GL.Enable(EnableCap.Lighting);
                            GL.PopMatrix();
                        }
                    }
                }
            }
            GL.Disable(EnableCap.Lighting);
            hoverText.Visible = middleDrag;
            if (middleDrag)
            {
                if (pick != null)
                {
                    var snap2 = SnapPoint(pick);
                    var snap1 = SnapPoint(startMeasurePick);
                    if (snap1 != null && snap2 != null)
                    {
                        GL.LineStipple(1, 0x3F07);
                        GL.LineWidth(3);
                        GL.Enable(EnableCap.LineStipple);
                        
                        GL.Color3(Color.Orange);
                        GL.Begin(PrimitiveType.Lines);
                        GL.Vertex3(snap1.Value);
                        GL.Vertex3(snap2.Value);
                        GL.End();
                        hoverText.Text = $"dist: {(snap1.Value - snap2.Value).Length:N4}";
                        GL.PointSize(10);
                        GL.Begin(PrimitiveType.Points);
                        GL.Vertex3(snap1.Value);
                        GL.Vertex3(snap2.Value);
                        GL.End();
                        GL.Disable(EnableCap.LineStipple);
                    }
                }
            }
            GL.Disable(EnableCap.Lighting);

            glControl.SwapBuffers();
        }

        Vector3d? SnapPoint(IntersectInfo inter)
        {
            float pickEps = 10;
            var pp = new[] {
                inter.Target.V0,
                inter.Target.V1,
                inter.Target.V2}.ToArray();
            if (pp.Any(uu => (uu - inter.Point).Length < pickEps))
            {
                return pp.OrderBy(uu => (uu - inter.Point).Length).First(uu => (uu - inter.Point).Length < pickEps);
            }
            return null;
        }

        DraftEditorControl de;
        Label hoverText;
        public Form1()
        {
            InitializeComponent();
            _currentTool = new SelectionTool(this);
            foreach (Control c in propertyGrid1.Controls)
            {
                c.MouseDoubleClick += C_MouseClick;
            }

            (treeListView1.Columns[0] as BrightIdeasSoftware.OLVColumn).AspectGetter = (x) =>
            {
                if (x is IDraftHelper dh)
                {
                    return $"<{x.GetType().Name}>";
                }
                if (x is IDrawable d)
                {
                    return d.Name;
                }

                return $"<{x.GetType().Name}>";
            };

            (treeListView1.Columns[1] as BrightIdeasSoftware.OLVColumn).AspectGetter = (x) =>
            {
                return $"{x.GetType().Name}";
            };

            treeListView1.ChildrenGetter = (x) =>
        {
            if (EditMode == EditModeEnum.Draft)
                if (x is Draft dd)
                {
                    var d1 = dd.Helpers.OfType<object>().ToArray();
                    var d2 = dd.Elements.OfType<object>().ToArray();
                    var d3 = dd.Constraints.OfType<object>().ToArray();
                    return d3.Union(d1.Union(d2)).ToArray();
                }
            if (x is Draft)
                return new object[] { };
            if (x is PartAssembly p)
            {
                return p.Parts.ToArray();
            }
            if (x is IDrawable d)
            {
                return d.Childs.ToArray();
            }


            return null;
        };
            treeListView1.CanExpandGetter = (x) =>
            {
                if (EditMode == EditModeEnum.Draft)
                    if (x is Draft dd)
                    {
                        var d1 = dd.Helpers.OfType<object>().ToArray();
                        var d3 = dd.Constraints.OfType<object>().ToArray();
                        var d2 = dd.Elements.OfType<object>().ToArray();
                        return d1.Length > 0 || d2.Length > 0 || d3.Length > 0;
                    }
                if (x is Draft)
                    return false;

                if (x is PartAssembly p)
                {
                    return p.Parts.Any();
                }
                if (x is IDrawable d)
                {
                    return d.Childs.Any();
                }


                return false;
            };

            treeListView1.AllowDrop = true;
            treeListView1.DragOver += TreeListView1_DragOver;
            treeListView1.DragEnter += TreeListView1_DragEnter;
            treeListView1.DragDrop += TreeListView1_DragDrop;
            treeListView1.ItemDrag += TreeListView1_ItemDrag;
            toolStrip2.Visible = false;
            toolStrip3.Visible = false;
            toolStrip1.Top = 0;
            toolStrip1.Left = 0;
            for (int i = 0; i < toolStripContainer1.TopToolStripPanel.Controls.Count; i++)
            {
                toolStripContainer1.TopToolStripPanel.Controls[i].Top = 0;
            }
            toolStrip2.Top = 0;
            toolStrip3.Top = 0;
            checkBox3.Checked = Part.AutoExtractMeshOnLoad;
            checkBox1.Checked = AllowPartLoadTimeout;
            Load += Form1_Load;
            Form = this;
            de = new DraftEditorControl();
            de.Init(this);
            de.Visible = false;
            de.Dock = DockStyle.Fill;

            glControl = new OpenTK.GLControl(new OpenTK.Graphics.GraphicsMode(32, 24, 0, 8));

            if (glControl.Context.GraphicsMode.Samples == 0)
            {
                glControl = new OpenTK.GLControl(new OpenTK.Graphics.GraphicsMode(32, 24, 0, 8));
            }

            hoverText = new Label();
            glControl.Controls.Add(hoverText);
            hoverText.Text = "dist:";
            hoverText.BackColor = Color.Transparent;
            hoverText.ForeColor = Color.White;
            hoverText.AutoSize = true;
            evwrapper = new EventWrapperGlControl(glControl);

            glControl.MouseDown += GlControl_MouseDown;
            glControl.MouseUp += GlControl_MouseUp;

            glControl.Paint += Gl_Paint;
            ViewManager = new DefaultCameraViewManager();
            ViewManager.Attach(evwrapper, camera1);

            panel2.Controls.Add(glControl);
            panel2.Controls.Add(de);
            panel2.Controls.Add(infoPanel);
            infoPanel.BringToFront();
            infoPanel.Dock = DockStyle.Bottom;

            glControl.Dock = DockStyle.Fill;
            DebugHelpers.Error = (str) =>
            {
                infoPanel.Invoke((Action)(() => { infoPanel.AddError(str); }));
            };
            DebugHelpers.Exception = (ex) =>
            {
                infoPanel.Invoke((Action)(() => { infoPanel.AddError(ex.Message, ex.StackTrace); }));
            };
            DebugHelpers.Warning = (str) =>
            {
                infoPanel.Invoke((Action)(() => { infoPanel.AddWarning(str); }));
            };
            DebugHelpers.Progress = (en, p) =>
            {
                infoPanel.Invoke((Action)(() =>
                {
                    toolStripProgressBar1.Visible = en;
                    toolStripProgressBar1.Value = (int)Math.Round(p);
                }));
            };

            infoPanel.Switch();
        }

        private void C_MouseClick(object sender, MouseEventArgs e)
        {
            var gi = propertyGrid1.SelectedGridItem;
            var obj = propertyGrid1.SelectedObject;
            if (gi.PropertyDescriptor.PropertyType == typeof(TransformationChain))
            {
                var ret = editorStart(gi.Value, gi.PropertyDescriptor.Name, typeof(Matrix4dPropEditor), false);
                gi.PropertyDescriptor.SetValue(obj, (TransformationChain)ret);
                //gi.PropertyDescriptor.Name
            }
            if (gi.PropertyDescriptor.PropertyType == typeof(Vector3d))
            {
                //editor call here
                //sert bacvk
                var ret = editorStart(gi.Value, gi.PropertyDescriptor.Name, typeof(Vector3dPropEditor));
                gi.PropertyDescriptor.SetValue(obj, (Vector3d)ret);
                //gi.PropertyDescriptor.Name
            }
        }

        internal void SetStatus(string v)
        {
            toolStripStatusLabel1.Text = v;
        }

        object editorStart(object init, string nm, Type control, bool dialog = true)
        {
            Form f = new Form() { Text = nm };
            f.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            f.StartPosition = FormStartPosition.CenterScreen;
            var cc = Activator.CreateInstance(control) as UserControl;
            (cc as IPropEditor).Init(init);
            f.Controls.Add(cc);
            f.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            f.AutoSize = true;
            if (dialog)
            {
                f.ShowDialog();
            }
            else
            {
                f.TopMost = true;
                f.Show();
            }
            return (cc as IPropEditor).ReturnValue;
        }

        private void TreeListView1_DragOver(object sender, DragEventArgs e)
        {
            // Retrieve the client coordinates of the mouse position.  
            // Point targetPoint = treeListView1.PointToClient(new Point(e.X, e.Y));

            // Select the node at the mouse position.  
            //treeListView1.SelectedNode = treeView1.GetNodeAt(targetPoint);
        }

        private void TreeListView1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.AllowedEffect;
        }

        public class MyDraggedData
        {
            public object Data;
            public MyDraggedData(object d)
            {
                Data = d;
            }
        }
        private void TreeListView1_DragDrop(object sender, DragEventArgs e)
        {
            Point targetPoint = treeListView1.PointToClient(new Point(e.X, e.Y));

            // Retrieve the node at the drop location.  
            var targetNode = (treeListView1.GetItemAt(targetPoint.X, targetPoint.Y) as BrightIdeasSoftware.OLVListItem).RowObject;

            // Retrieve the node that was dragged.              
            var draggedNode = (MyDraggedData)e.Data.GetData(typeof(MyDraggedData));
            if (draggedNode != null)
            {
                var dr = draggedNode.Data as IDrawable;
                if (targetNode is PartAssembly pas)
                {
                    if (dr is IPartContainer pc)
                        pas.AddPart(new PartInstance(pc));
                    if (dr is Part p)
                        pas.AddPart(new PartInstance(p));//create LinkReference to part instance
                }
                if (targetNode is Group gr)
                {
                    if (dr.Parent != null)
                        dr.Parent.RemoveChild(dr);
                    else
                        Parts.Remove(dr);
                    if (!gr.Childs.Contains(dr))
                    {
                        //check nesting
                        gr.Childs.Add(dr);
                        dr.Parent = gr;
                    }

                }
            }
            updateList();
        }

        private void TreeListView1_ItemDrag(object sender, ItemDragEventArgs e)
        {
            var item = e.Item as BrightIdeasSoftware.OLVListItem;
            DoDragDrop(new MyDraggedData(item.RowObject), DragDropEffects.Copy);
            //throw new NotImplementedException();
        }

        private void GlControl_MouseUp(object sender, MouseEventArgs e)
        {
            middleDrag = false;
            CurrentTool.MouseUp(e);
        }

        bool middleDrag = false;
        IntersectInfo startMeasurePick = null;
        private void GlControl_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle)
            {
                if (pick != null)
                {
                    middleDrag = true;
                    startMeasurePick = pick;
                }
            }

            CurrentTool.MouseDown(e);
        }

        InfoPanel infoPanel = new InfoPanel();

        private void timer1_Tick(object sender, EventArgs e)
        {
            glControl.Invalidate();
            label1.Text = "camera len: " + camera1.DirLen;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            camera1.CamTo = Vector3.Zero;
            camera1.CamFrom = new Vector3(-10, 0, 0);
            camera1.CamUp = Vector3.UnitZ;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            camera1.CamTo = Vector3.Zero;
            camera1.CamFrom = new Vector3(0, -10, 0);
            camera1.CamUp = Vector3.UnitZ;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            camera1.CamTo = Vector3.Zero;
            camera1.CamFrom = new Vector3(0, 0, -10);
            camera1.CamUp = Vector3.UnitY;
        }


        public bool AllowPartLoadTimeout = true;
        public int PartLoadTimeout = 15000;
        bool loaded = false;

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "LiteCAD scene (*.lcs)|*.lcs";
            if (ofd.ShowDialog() != DialogResult.OK) return;
            Scene = new LiteCADScene();
            Scene.FromXml(ofd.FileName);
            updateList();
        }

        private void planeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PlaneHelper ph = new PlaneHelper() { Normal = Vector3d.UnitZ };
            Parts.Add(ph);
            ph.Name = "plane01";
            updateList();
        }
        Part selectedPart;
        private void treeListView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (treeListView1.SelectedObject == null) return;
            var tag = treeListView1.SelectedObject;
            propertyGrid1.SelectedObject = tag;
            if (tag is Part part)
            {
                updateFacesList(part);
                selectedPart = part;
            }
            if (tag is IEditFieldsContainer c)
            {
                var objs = c.GetObjects();
                listView1.Items.Clear();
                foreach (var item in objs)
                {
                    listView1.Items.Add(new ListViewItem(new string[] { item.Name }) { Tag = item });
                }
            }
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0) return;
            propertyGrid1.SelectedObject = listView1.SelectedItems[0].Tag;
        }

        public void FitToPoints(Vector3d[] pnts, Camera cam, float gap = 10)
        {
            List<Vector2d> vv = new List<Vector2d>();
            foreach (var vertex in pnts)
            {
                var p = MouseRay.Project(vertex.ToVector3(), cam.ProjectionMatrix, cam.ViewMatrix, cam.WorldMatrix, camera1.viewport);
                vv.Add(p.Xy.ToVector2d());
            }

            //prjs->xy coords
            var minx = vv.Min(z => z.X) - gap;
            var maxx = vv.Max(z => z.X) + gap;
            var miny = vv.Min(z => z.Y) - gap;
            var maxy = vv.Max(z => z.Y) + gap;

            var dx = (maxx - minx);
            var dy = (maxy - miny);

            var cx = dx / 2;
            var cy = dy / 2;
            var dir = cam.CamTo - cam.CamFrom;
            //center back to 3d

            var mr = new MouseRay((float)(cx + minx), (float)(cy + miny), cam);
            var v0 = mr.Start;

            cam.CamFrom = v0;
            cam.CamTo = cam.CamFrom + dir;

            var aspect = glControl.Width / (float)(glControl.Height);

            dx /= glControl.Width;
            dx *= camera1.OrthoWidth;
            dy /= glControl.Height;
            dy *= camera1.OrthoWidth;

            cam.OrthoWidth = (float)Math.Max(dx, dy);
        }
        Vector3d[] getAllPoints()
        {
            var t1 = Parts.OfType<IMesh>().ToArray();
            var ad = Parts.OfType<AbstractDrawable>().ToArray();
            var t2 = ad.SelectMany(z => z.GetAll((xx) => xx is IMesh)).OfType<IMesh>();
            var p1 = getAllPoints(t1.Union(t2).ToArray());

            //var p1 = getAllPoints(Parts.OfType<Part>().ToArray());
            return p1;
            //var drafts = Parts.OfType<Draft>();
            // var p2 = drafts.SelectMany(z => z.DraftPoints.Select(u => u.Location));

            // return p2.Union(p1).ToArray();
        }

        Vector3d[] getAllPoints(IMesh[] parts)
        {
            List<Vector3d> vv = new List<Vector3d>();
            foreach (var p in parts)
            {
                vv.AddRange(p.GetPoints());
                /*var nn = p.Nodes.SelectMany(z => z.Triangles.SelectMany(u => u.Vertices.Select(zz => zz.Position))).ToArray();
                vv.AddRange(nn);
                foreach (var face in p.Faces)
                {
                    foreach (var ditem in face.Items)
                    {
                        if (ditem is LineItem li)
                        {
                            vv.Add(li.Start);
                            vv.Add(li.End);
                        }
                    }
                }*/
            }
            return vv.ToArray();
        }

        void fitAll(Vector3d[] vv = null)
        {
            if (EditMode == EditModeEnum.Draft)
            {
                de.FitAll();
                return;
            }
            if (vv == null)
                vv = getAllPoints();
            if (vv.Length == 0) return;
            FitToPoints(vv, camera1);
        }

        BRepFace selectedFace = null;
        void updateFacesList(Part part)
        {
            listView2.Items.Clear();
            foreach (var item in part.Faces)
            {
                listView2.Items.Add(new ListViewItem(new string[] { item.Id.ToString(), item.Surface.GetType().Name, item.Visible.ToString() }) { Tag = item });
            }
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            fitAll();
        }


        void deleteItem()
        {
            if (treeListView1.SelectedObjects.Count <= 0) return;
            if (GUIHelpers.ShowQuestion($"Are you sure to delete {treeListView1.SelectedObjects.Count} items?", Text) != DialogResult.Yes) return;
            foreach (var item in treeListView1.SelectedObjects)
            {
                if (editedDraft == item)
                {
                    GUIHelpers.Warning("you can't delete edited draft", Text);
                    continue;
                }

                if (item is DraftElement de)
                {
                    de.Parent.RemoveElement(de);
                }
                if (item is IDrawable dd)
                {
                    if (dd.Parent == null)
                    {
                        Parts.Remove(item as IDrawable);
                    }
                    else
                    {
                        dd.Parent.RemoveChild(dd);
                    }
                }
            }
            updateList();
        }
        void cloneItem()
        {
            if (treeListView1.SelectedObjects.Count <= 0) return;

            foreach (var item in treeListView1.SelectedObjects)
            {
                if (item is PartInstance pi)
                {
                    PartInstance pp = new PartInstance(pi.Part);
                    pp.Name = pi.Name + "_cloned";
                    pp.Matrix = pi.Matrix.Clone();
                    pp.Color = pi.Color;
                    if (pi.Parent != null)
                    {
                        pp.Parent = pi.Parent;
                        if (pp.Parent is PartAssembly)
                            pp.Parent.Childs.Add(pp);
                    }
                    else
                    {
                        Parts.Add(pp);
                    }
                    updateList();
                }

            }
            updateList();
        }
        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            deleteItem();
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            foreach (var item in Parts.OfType<Part>())
            {
                item.ShowNormals = checkBox2.Checked;
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (!(treeListView1.SelectedObject is Part pp)) return;
            pp.FixNormals();
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            Part.AutoExtractMeshOnLoad = checkBox3.Checked;
        }

        private void button7_Click(object sender, EventArgs e)
        {
            var len = camera1.DirLen;
            var dir = camera1.Dir;
            camera1.CamTo = Vector3.Zero;
            camera1.CamFrom = camera1.CamTo + dir * len;
        }


        void camToSelected(Vector3d[] vv)
        {
            if (vv == null || vv.Length == 0) return;
            Vector3d cnt = Vector3d.Zero;
            foreach (var item in vv)
            {
                cnt += item;
            }
            cnt /= vv.Length;
            var len = camera1.DirLen;
            var dir = camera1.Dir;
            camera1.CamTo = cnt.ToVector3();
            camera1.CamFrom = camera1.CamTo + dir * len;
        }
        private void button6_Click(object sender, EventArgs e)
        {
            var vv = getAllPoints();
            camToSelected(vv);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            if (!(treeListView1.SelectedObject is Part pp)) return;
            pp.ExtractMesh();
        }

        private void listView2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (selectedFace != null)
            {
                selectedFace.Selected = false;
            }
            if (listView2.SelectedItems.Count == 0) { return; }

            selectedFace = listView2.SelectedItems[0].Tag as BRepFace;
            selectedFace.Selected = true;
        }

        private void visibleSwitchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView2.SelectedItems.Count == 0) return;
            var face = listView2.SelectedItems[0].Tag as BRepFace;
            face.Visible = !face.Visible;
            updateFacesList(selectedPart);
        }

        private void checkBox1_CheckedChanged_1(object sender, EventArgs e)
        {
            AllowPartLoadTimeout = checkBox1.Checked;
        }

        private void updateMeshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView2.SelectedItems.Count == 0) return;
            var face = listView2.SelectedItems[0].Tag as BRepFace;
            face.ExtractMesh();
        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            drawAxes = checkBox5.Checked;
        }

        private void switchNormalsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView2.SelectedItems.Count == 0) return;
            var face = listView2.SelectedItems[0].Tag as BRepFace;
            face.Node.SwitchNormal();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Decimal)
            {
                camToSelected(new[] { lastHovered });
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void treeListView1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                deleteItem();
            }
          
        }

        private void checkBox6_CheckedChanged(object sender, EventArgs e)
        {
            StepParseContext.DebugInfoEnabled = checkBox6.Checked;
        }

        private void checkBox7_CheckedChanged(object sender, EventArgs e)
        {
            StepParseContext.SkipFaceOnException = checkBox7.Checked;
        }

        private void updateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView2.SelectedItems.Count == 0) return;
            var face = listView2.SelectedItems[0].Tag as BRepFace;
            face.ExtractMesh();
        }

        private void deleteToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (listView2.SelectedItems.Count == 0) return;
            var face = listView2.SelectedItems[0].Tag as BRepFace;
            face.Node = null;
        }

        private void button9_Click(object sender, EventArgs e)
        {
            if (!(treeListView1.SelectedObject is Part pp)) return;
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "PLY files (*.ply)|*.ply";
            if (sfd.ShowDialog() != DialogResult.OK) return;
            List<Vector3d> ret = new List<Vector3d>();
            foreach (var item in pp.Nodes)
            {
                foreach (var t in item.Triangles)
                {
                    foreach (var v in t.Vertices)
                    {
                        ret.Add(v.Position);
                    }
                }
            }

            PlyStuff.Save(sfd.FileName, ret.ToArray());
        }

        private void checkBox8_CheckedChanged(object sender, EventArgs e)
        {
            BRepFace.SkipWireOnParseException = checkBox8.Checked;
        }

        private void projectionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView2.SelectedItems.Count == 0) return;
            var face = listView2.SelectedItems[0].Tag as BRepFace;
            ProjectMapEditor pme = new ProjectMapEditor();
            pme.Init(face);
            pme.ShowDialog();

            if (pme.Exception != null)
            {
                infoPanel.AddError(pme.Exception.Message);
            }
        }

        private void fitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView2.SelectedItems.Count == 0) return;
            var face = listView2.SelectedItems[0].Tag as BRepFace;
            List<Vector3d> vv = new List<Vector3d>();

            foreach (var ditem in face.Items)
            {
                if (ditem is LineItem li)
                {
                    vv.Add(li.Start);
                    vv.Add(li.End);
                }
            }

            if (vv.Count == 0) return;
            camToSelected(vv.ToArray());
            FitToPoints(vv.ToArray(), camera1);
            camToSelected(vv.ToArray());
        }

        private void draftToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Draft draft = new Draft() { Name = "new draft" };
            Parts.Add(draft);
            updateList();
        }

        Vector3[] camState = new Vector3[3];

        private void editToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeListView1.SelectedObjects.Count <= 0) return;
            if (treeListView1.SelectedObject is Draft d)
            {
                glControl.Visible = false;
                de.Visible = true;
                de.SetDraft(d);
                de.FitAll();

                //   panel2.Controls.Add(glControl);

                EditMode = EditModeEnum.Draft;
                updateList();
                camState[0] = camera1.CamTo;
                camState[1] = camera1.CamFrom;
                camState[2] = camera1.CamUp;

                camera1.CamTo = d.Plane.Position.ToVector3();
                camera1.CamFrom = (d.Plane.Position + d.Plane.Normal * 10).ToVector3();
                camera1.CamUp = Vector3.UnitY;

                editedDraft = d;
                toolStrip2.Visible = true;
                toolStrip3.Visible = true;
            }

        }

        public EditModeEnum EditMode;
        ITool _currentTool;
        public void SetTool(ITool tool)
        {
            _currentTool.Deselect();
            _currentTool = tool;
            _currentTool.Select();
            ToolChanged?.Invoke(_currentTool);
        }
        public ITool CurrentTool { get => _currentTool; }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            SetTool(new DraftLineTool(de));
            uncheckedAllToolButtons();
            toolStripButton2.Checked = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }
        Draft editedDraft;
        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            SetTool(new RectDraftTool(de));
            uncheckedAllToolButtons();
            toolStripButton3.Checked = true;
        }

        private void toolStripButton8_Click(object sender, EventArgs e)
        {
            EditMode = EditModeEnum.Part;
            updateList();

            de.Visible = false;
            de.Finish();
            glControl.Visible = true;
            SetTool(new SelectionTool(this));
            editedDraft = null;
            toolStrip2.Visible = false;
            toolStrip3.Visible = false;
            //restore cam state
            camera1.CamTo = camState[0];
            camera1.CamFrom = camState[1];
            camera1.CamUp = camState[2];
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            camera1.CamTo = Vector3.Zero;
            camera1.CamFrom = new Vector3(250, 250, 250);
            camera1.CamUp = Vector3.UnitZ;
        }
        public event Action<ITool> ToolChanged;
        private void fitToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (treeListView1.SelectedObjects.Count <= 0) return;
            //var vv = getAllPoints(treeListView1.SelectedObjects.OfType<IMesh>().ToArray());
            var t1 = treeListView1.SelectedObjects.OfType<IMesh>().ToArray();
            var ad = treeListView1.SelectedObjects.OfType<AbstractDrawable>().ToArray();
            var t2 = ad.SelectMany(z => z.GetAll((xx) => xx is IMesh)).OfType<IMesh>();
            var vv = getAllPoints(t1.Union(t2).ToArray());

            fitAll(vv);
            camToSelected(vv);
        }

        private void toolStripButton9_Click(object sender, EventArgs e)
        {
            SetTool(new SelectionTool(this));
            uncheckedAllToolButtons();
            toolStripButton18.Checked = true;
        }

        void uncheckedAllToolButtons()
        {
            toolStripButton9.Checked = false;
            toolStripButton18.Checked = false;
            toolStripButton2.Checked = false;
            toolStripButton3.Checked = false;
            toolStripButton4.Checked = false;
            toolStripButton17.Checked = false;

        }

        private void toolStripButton10_Click(object sender, EventArgs e)
        {
            de.Clear();
        }

        private void toolStripButton11_Click(object sender, EventArgs e)
        {
            de.CloseLine();
        }

        private void toolStripButton5_Click(object sender, EventArgs e)
        {
            SetTool(new LinearConstraintTool(de));
        }

        public void ResetTool()
        {
            uncheckedAllToolButtons();
            SetTool(new SelectionTool(this));
            toolStripButton9.Checked = true;
        }

        public void ObjectSelect(object nearest)
        {
            propertyGrid1.SelectedObject = nearest;
        }

        void exportObj(Part part)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "OBJ model (*.obj)|*.obj";
            if (sfd.ShowDialog() != DialogResult.OK) return;

            using (var fs = new FileStream(sfd.FileName, FileMode.CreateNew))
            {
                foreach (var item in part.Nodes)
                {
                    //item.Triangles
                }
            }
        }

        void exportDxf(Draft draft)
        {
            //export to dxf
            IxMilia.Dxf.DxfFile file = new IxMilia.Dxf.DxfFile();
            foreach (var item in draft.DraftLines)
            {
                if (item.Dummy)
                    continue;

                file.Entities.Add(new DxfLine(new DxfPoint(item.V0.X, item.V0.Y, 0), new DxfPoint(item.V1.X, item.V1.Y, 0)));
            }
            foreach (var item in draft.DraftEllipses)
            {
                //file.Entities.Add(new DxfEllipse(new DxfPoint(item.Center.X, item.Center.Y, 0), new DxfVector((double)item.Radius, 0, 0), 360));
                file.Entities.Add(new DxfCircle(new DxfPoint(item.Center.X, item.Center.Y, 0), (double)item.Radius));
                //file.Entities.Add(new DxfArc(new DxfPoint(item.Center.X, item.Center.Y, 0), (double)item.Radius, 0, 360));
            }
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "DXF files (*.dxf)|*.dxf";

            if (sfd.ShowDialog() != DialogResult.OK) return;
            file.Save(sfd.FileName);
        }
        private void toolStripButton7_Click(object sender, EventArgs e)
        {
            exportDxf(editedDraft);
        }

        private void partToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "STEP files (*.stp;*.step)|*.stp;*.step|All files (*.*)|*.*";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                loaded = false;
                Thread th = new Thread(() =>
                {
                    try
                    {
                        Part prt = StepParser.Parse(ofd.FileName);
                        var fi = new FileInfo(ofd.FileName);
                        loaded = true;
                        lock (Parts)
                        {
                            Parts.Add(prt);
                        }
                        infoPanel.AddInfo($"model loaded succesfully: {fi.Name}");
                        updateList();
                        var vv = getAllPoints();
                        fitAll(vv);
                        camToSelected(vv);
                    }
                    catch (Exception ex)
                    {
                        loaded = true;
                        DebugHelpers.Exception(ex);
                    }
                });
                Thread th2 = new Thread(() =>
                {
                    Stopwatch sw = Stopwatch.StartNew();
                    while (true)
                    {
                        if (!Debugger.IsAttached && sw.Elapsed.TotalSeconds > PartLoadTimeout)
                        {
                            th.Abort();
                            DebugHelpers.Error("load timeout");
                            break;
                        }
                        Thread.Sleep(1000);
                        if (loaded) break;
                    }
                });
                if (!Debugger.IsAttached && AllowPartLoadTimeout)
                {
                    th2.IsBackground = true;
                    th2.Start();
                }
                th.IsBackground = true;
                th.Start();
            }
        }

        void updateList()
        {
            /*var grp = new Group() { Name = "root" };
            grp.Childs.AddRange(Parts);
            treeListView1.SetObjects(new[] { grp});*/
            treeListView1.SetObjects(Parts);
        }
        private void toolStripButton6_Click(object sender, EventArgs e)
        {
            if (treeListView1.SelectedObject is Draft dd)
            {
                Parts.Remove(dd);
                Parts.Add(new ExtrudeModifier(dd) { });
                updateList();
            }
        }

        private void partToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        public LiteCADScene Scene = new LiteCADScene();
        public List<IDrawable> Parts => Scene.Parts;

        IDrawable[] IEditor.Parts => Parts.ToArray();

        private void assemblyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Parts.Add(new PartAssembly());
            updateList();
        }

        private void updateToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            updateList();
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog ofd = new SaveFileDialog();
            ofd.Filter = "LiteCAD scene (*.lcs)|*.lcs";
            if (ofd.ShowDialog() != DialogResult.OK) return;
            Scene.SaveToXml(ofd.FileName);
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            SetTool(new DraftEllipseTool(this));
            uncheckedAllToolButtons();
            toolStripButton4.Checked = true;
        }

        private void dxfToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeListView1.SelectedObjects.Count <= 0) return;
            var vv = treeListView1.SelectedObjects.OfType<Draft>().ToArray();
            exportDxf(vv[0]);
        }

        private void objToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeListView1.SelectedObjects.Count <= 0) return;
            var vv = treeListView1.SelectedObjects.OfType<Part>().ToArray();
            if (vv.Any())
            {
                exportObj(vv[0]);
            }
            else
            {
                var vv2 = treeListView1.SelectedObjects.OfType<ExtrudeModifier>().ToArray();
                if (vv2.Any() && vv2[0].Part != null)
                {
                    exportObj(vv2[0].Part);
                }
            }
        }

        private void infoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeListView1.SelectedObjects.Count <= 0) return;
            var vv = treeListView1.SelectedObjects.OfType<IEconomicsDetail>().ToArray();
            var vv4 = treeListView1.SelectedObjects.OfType<PartAssembly>().ToArray();
            var vv2 = treeListView1.SelectedObjects.OfType<PartInstance>().ToArray();
            var vv3 = vv2.Where(z => z.Part is IEconomicsDetail).Select(z => (IEconomicsDetail)z.Part).OfType<IEconomicsDetail>().ToArray();
            vv = vv.Union(vv3).ToArray();
            if (vv.Length == 0)
            {
                if (vv4.Any())
                {
                    Info inf2 = new Info();
                    inf2.Init(vv4[0].GetAll(x => x is IEconomicsDetail).OfType<IEconomicsDetail>().ToArray());
                    inf2.Show(this);
                }
                return;
            }
            Info inf = new Info();
            inf.Init(vv[0]);
            inf.Show(this);
        }

        private void toolStripButton14_Click(object sender, EventArgs e)
        {
            SetTool(new PerpendicularConstraintTool(de));
        }

        private void toolStripButton12_Click(object sender, EventArgs e)
        {
            SetTool(new ParallelConstraintTool(this));
        }

        private void meshToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            //obj or stl
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "STL file (*.stl)|*.stl";
            if (ofd.ShowDialog() != DialogResult.OK) return;
            var bts = File.ReadAllBytes(ofd.FileName);
            var cnt = BitConverter.ToInt32(bts, 80);
            MeshModel mm = new MeshModel() { Name = Path.GetFileNameWithoutExtension(ofd.FileName) };
            MeshNode node = new MeshNode();
            mm.Nodes.Add(node);
            Parts.Add(mm);


            for (int i = 0; i < cnt; i++)
            {
                TriangleInfo tr = new TriangleInfo();
                node.Triangles.Add(tr);

                tr.Vertices = new VertexInfo[3];
                Vector3d normal = new Vector3d();
                Vector3d v1 = new Vector3d();
                Vector3d v2 = new Vector3d();
                Vector3d v3 = new Vector3d();

                for (int j = 0; j < 3; j++)
                {
                    var fl = BitConverter.ToSingle(bts, 84 + j * 4 + i * 50);
                    normal[j] = fl;
                }
                for (int j = 0; j < 3; j++)
                {
                    var fl = BitConverter.ToSingle(bts, 84 + 12 + j * 4 + i * 50);
                    v1[j] = fl;
                }

                for (int j = 0; j < 3; j++)
                {
                    var fl = BitConverter.ToSingle(bts, 84 + 24 + j * 4 + i * 50);
                    v2[j] = fl;
                }
                for (int j = 0; j < 3; j++)
                {
                    var fl = BitConverter.ToSingle(bts, 84 + 36 + j * 4 + i * 50);
                    v3[j] = fl;
                }
                tr.Vertices[0] = new VertexInfo() { Position = v1, Normal = normal };
                tr.Vertices[1] = new VertexInfo() { Position = v2, Normal = normal };
                tr.Vertices[2] = new VertexInfo() { Position = v3, Normal = normal };


            }
            updateList();
        }

        private void propertyGrid1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            var gi = propertyGrid1.SelectedGridItem;
        }

        private void propertyGrid1_DoubleClick(object sender, EventArgs e)
        {

        }

        private void cutAllByPlaneToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var models = treeListView1.SelectedObjects.OfType<IPlaneSplittable>().ToArray();
            if (models.Length == 0) return;
            var ph = treeListView1.SelectedObjects.OfType<PlaneHelper>().FirstOrDefault();
            if (ph == null) return;

            var pnts = models.SelectMany(z => z.SplitPyPlane(ph));

            //project all to ph
            //var proj = pnts.Select(ph.ProjPoint).ToList();
            var lines = pnts.ToList();
            Draft d = new Draft() { Name = "cut-by-plane" };
            float closeEps = 1e-3f;
            /*Group gr = new Group() { Name = "cut-by-plane-lines" };
            Parts.Add(gr);
            foreach (var item in lines)
            {
                gr.Childs.Add(new LineHelper(item));
            }*/
            foreach (var line in lines)
            {
                var uv1 = ph.ProjectPointUV(line.Start);
                var uv2 = ph.ProjectPointUV(line.End);
                var fd1 = d.DraftPoints.FirstOrDefault(z => (z.Location - uv1).Length < closeEps);
                var fd2 = d.DraftPoints.FirstOrDefault(z => (z.Location - uv2).Length < closeEps);
                if (fd1 == null)
                {
                    fd1 = new DraftPoint(d, uv1.X, uv1.Y);
                    d.AddElement(fd1);
                }
                if (fd2 == null)
                {
                    fd2 = new DraftPoint(d, uv2.X, uv2.Y);
                    d.AddElement(fd2);
                }

                d.AddElement(new DraftLine(fd1, fd2, d));
            }

            Parts.Add(d);
            updateList();
        }

        private void groupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Parts.Add(new Group());
            updateList();
        }

        private void toolStripButton15_Click(object sender, EventArgs e)
        {
            de.FlipHorizontal();
        }

        private void toolStripButton16_Click(object sender, EventArgs e)
        {
            de.Undo();
        }

        private void toolStripButton17_Click(object sender, EventArgs e)
        {

        }

        private void toolStripButton18_Click(object sender, EventArgs e)
        {

        }

        private void toolStripButton19_Click(object sender, EventArgs e)
        {
            SetTool(new HorizontalConstraintTool(de));
        }

        private void toolStripButton20_Click(object sender, EventArgs e)
        {
            SetTool(new VerticalConstraintTool(de));
        }

        private void setCameraToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeListView1.SelectedObjects.Count <= 0) return;
            var t1 = treeListView1.SelectedObjects.OfType<IMesh>().ToArray();
            var ad = treeListView1.SelectedObjects.OfType<AbstractDrawable>().ToArray();
            var t2 = ad.SelectMany(z => z.GetAll((xx) => xx is IMesh)).OfType<IMesh>();
            var vv = getAllPoints(t1.Union(t2).ToArray());
            camToSelected(vv);
        }

        private void cloneToolStripMenuItem_Click(object sender, EventArgs e)
        {
            cloneItem();
        }

        private void toolStripButton17_Click_1(object sender, EventArgs e)
        {
            SetTool(new AdjoinTool(this));
            uncheckedAllToolButtons();
            toolStripButton17.Checked = true;
        }

        private void toolStripButton18_Click_1(object sender, EventArgs e)
        {
            SetTool(new SelectionTool(this));
            uncheckedAllToolButtons();
            toolStripButton18.Checked = true;
        }

        private void extract3dContourToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView2.SelectedItems.Count == 0) return;
            var face = listView2.SelectedItems[0].Tag as BRepFace;
            List<Line3D> ll = new List<Line3D>();
            foreach (var item in face.Wires)
            {
                var cf = (face as BRep.Faces.BRepCylinderSurfaceFace);
                if (cf != null)
                {
                    var ee = item.Edges.SelectMany(z => cf.get3DSegments(z)).ToArray();
                    ll.AddRange(ee);
                }
            }
            PolylineHelper p = new PolylineHelper(ll.ToArray());
            Parts.Add(p);
            updateList();
        }
    }

    public enum EditModeEnum
    {
        Part, Draft, Assembly
    }
    public interface IPropEditor
    {
        void Init(object o);
        object ReturnValue { get; }
    }

}