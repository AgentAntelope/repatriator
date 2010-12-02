﻿using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Media3D;
using System.Windows.Media;
using System.Linq;
using System;
using System.Windows.Input;

namespace repatriator_client
{
    public partial class ButterflyControl : UserControl
    {
        // angles are in degrees. oh well.
        private const double pixelsToDegrees = 1.0;
        private int angleX = 180;
        private int angleY = 180;

        public int AngleX
        {
            get { return angleX; }
        }
        public int AngleY
        {
            get { return angleY; }
        }

        public event Action AnglesMoved;

        public ButterflyControl()
        {
            InitializeComponent();

            refreshTransforms();
        }

        private bool dragging = false;
        private System.Drawing.Point lockCursorPosition;
        private System.Drawing.Point restoreCursorPosition;

        private void mouseIntercepterCanvas_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (dragging)
                return; // this should never happen
            startDragging();
        }
        private void mouseIntercepterCanvas_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!dragging)
                return;
            stopDragging();
        }
        private void mouseIntercepterCanvas_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!dragging)
                return;
            // I would love to treat leaving the same as moving. It would prevent the user from escaping our grasp.
            // However, I couldn't get it to behave reliably and I didn't take the time to figure out how to make it work right.
            // Treating leaving as stopping works reliably, although it's probably more annoying to users.
            stopDragging();
        }
        private void mouseIntercepterCanvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!dragging)
                return;
            reactToDragging();
        }


        private void startDragging()
        {
            dragging = true;

            // store the center of the control
            System.Windows.Point relativeCenter = new System.Windows.Point(mouseIntercepterCanvas.ActualWidth / 2, mouseIntercepterCanvas.ActualHeight / 2);
            System.Windows.Point absoluteCenter = PointToScreen(relativeCenter);
            lockCursorPosition = new System.Drawing.Point((int)absoluteCenter.X, (int)absoluteCenter.Y);

            // hide the cursor
            System.Windows.Forms.Cursor.Hide();

            // remember the user's cursor position so we can restore it later
            restoreCursorPosition = System.Windows.Forms.Cursor.Position;
            // start with the cursor in the center
            System.Windows.Forms.Cursor.Position = lockCursorPosition;
        }
        private void reactToDragging()
        {
            // calculate the movement (in absolute coordinates)
            System.Drawing.Point currentMouseLocation = System.Windows.Forms.Cursor.Position;
            int deltaX = currentMouseLocation.X - lockCursorPosition.X;
            int deltaY = currentMouseLocation.Y - lockCursorPosition.Y;
            int angleDeltaX = (int)(deltaX * pixelsToDegrees);
            int angleDeltaY = (int)(deltaY * pixelsToDegrees);
            updateAngles(angleDeltaX, angleDeltaY);
            // hold the cursor in the center of the control
            System.Windows.Forms.Cursor.Position = lockCursorPosition;

            refreshTransforms();
        }
        private void updateAngles(int angleDeltaX, int angleDeltaY)
        {
            angleX = Utils.wrapOverflow(angleX + angleDeltaX, 360);
            angleY = Utils.wrapOverflow(angleY + angleDeltaY, 360);
            if (AnglesMoved != null)
                AnglesMoved();
        }
        private void stopDragging()
        {
            dragging = false;

            // restore position of cursor to where the user wanted it
            System.Windows.Forms.Cursor.Position = restoreCursorPosition;

            // show the cursor
            System.Windows.Forms.Cursor.Show();
        }

        private void refreshTransforms()
        {
            innerRotation.Angle = angleY;
            outterRotation.Angle = angleX;
        }
    }
}
