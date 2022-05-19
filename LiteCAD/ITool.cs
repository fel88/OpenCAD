﻿using System.Windows.Forms;

namespace LiteCAD
{
    public interface ITool
    {
        void Update();
        void MouseDown(MouseEventArgs e);
        void MouseUp(MouseEventArgs e);
        void Draw();
        void Select();
        void Deselect();
    }
}