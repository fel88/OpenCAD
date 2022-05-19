﻿using LiteCAD.Common;
using LiteCAD.DraftEditor;
using System.Linq;
using System.Windows.Forms;

namespace LiteCAD.Tools
{
    public class HorizontalConstraintTool : AbstractTool
    {
        public static HorizontalConstraintTool Instance = new HorizontalConstraintTool();

        public override void Deselect()
        {

        }

        public override void Draw()
        {

        }

        public override void MouseDown(MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            if (Editor.nearest is DraftLine dl)
            {
                var cc = new HorizontalConstraint(dl);

                if (!Editor.Draft.Constraints.OfType<HorizontalConstraint>().Any(z => z.IsSame(cc)))
                {
                    Editor.Draft.AddConstraint(cc);
                    Editor.Draft.AddHelper(new HorizontalConstraintHelper(cc));
                    Editor.Draft.Childs.Add(Editor.Draft.Helpers.Last());
                }
                else
                {
                    GUIHelpers.Warning("such constraint already exist");
                }

                //Form1.Form.ResetTool();

            }

        }

        public override void MouseUp(MouseEventArgs e)
        {

        }

        public override void Select()
        {


        }

        public override void Update()
        {

        }
    }
}