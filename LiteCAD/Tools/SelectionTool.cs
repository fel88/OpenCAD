﻿using System.Windows.Forms;

namespace LiteCAD.Tools
{
    public class SelectionTool : AbstractTool
    {
        public static SelectionTool Instance = new SelectionTool();

        public override void Deselect()
        {
            Form1.Form.ViewManager.Detach();
        }

        public override void Draw()
        {
            
        }

        public override void MouseDown(MouseEventArgs e)
        {
            
        }

        public override void MouseUp(MouseEventArgs e)
        {
            
        }

        public override void Select()
        {
            Form1.Form.ViewManager.Attach(Form1.Form.evwrapper, Form1.Form.camera1);

        }

        public override void Update()
        {
            
        }
    }
}