﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SimpleVideoCutter
{
    public partial class VideoCutterTimeline : UserControl
    {
        public event EventHandler<TimelineHoverEventArgs> TimelineHover;
        public event EventHandler<SelectionChangedEventArgs> SelectionChanged;
        public event EventHandler<PositionChangeRequestEventArgs> PositionChangeRequest;

        private Brush brushBackground = new SolidBrush(Color.FromArgb(0x4C, 0x4C, 0x4C));
        private Brush brushBackgroundInfoArea = new SolidBrush(Color.FromArgb(0x5C, 0x5C, 0x5C));
        private Brush brushBackgroundSelected = new SolidBrush(Color.FromArgb(0xAD, 0xAD, 0xAD));
        //private Pen penTick = new Pen(Color.FromArgb(0x5A, 0x5A, 0x5A));
        private Pen penTick = new Pen(Color.Snow);
        private Brush brushHoverPosition = new SolidBrush(Color.FromArgb(0xC8, 0x17, 0x17));
        private Brush brushSelectionMarker = new SolidBrush(Color.FromArgb(0xFF, 0xE9, 0x7F));
        private Brush brushPosition = new SolidBrush(Color.FromArgb(0x00, 0x5C, 0x9E));

        private PositionMoveController selectionStartMoveController;
        private PositionMoveController selectionEndMoveController;

        private long position = 0;
        private long? hoverPosition = null;
        private long? selectionStart = null;
        private long? selectionEnd = null;

        private float scale = 1.0f;
        private long offset = 0;

        public long Length { get; set; }
        
        public long Position
        {
            get
            {
                return position;
            }
            set
            {
                position = value;

                if (PositionToPixel(position) > ClientRectangle.Width)
                {
                    var newOffset = position;
                    if (newOffset + ClientRectangle.Width * MillisecondsPerPixels() > Length)
                        newOffset = Length - (long)(ClientRectangle.Width * MillisecondsPerPixels());
                    offset = newOffset;
                }

                Refresh();
            }
        }

        public long? HoverPosition
        {
            get
            {
                return hoverPosition;
            }
            set
            {
                if (hoverPosition == value)
                    return; 

                hoverPosition = value;
                Invalidate();
                TimelineHover?.Invoke(this, new TimelineHoverEventArgs());
            }
        }

        public long? SelectionStart
        {
            get { return selectionStart; }
        }
        public long? SelectionEnd
        {
            get { return selectionEnd; }
        }

        public VideoCutterTimeline()
        {
            InitializeComponent();
            selectionStartMoveController = new SelectionStartMoveController(this);
            selectionEndMoveController = new SelectionEndMoveController(this);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            if (ModifierKeys == Keys.Control)
            {
                float newScale = scale + (e.Delta / SystemInformation.MouseWheelScrollDelta * 0.25f);

                if (newScale < 1)
                    newScale = 1;

                if (newScale > scale)
                {
                    // When zooming we try to preserve hovered point in the same place 
                    var hoveredPosition = PixelToPosition(e.X);
                }
                else if (newScale < scale)
                {

                }

                scale = newScale;
                Refresh();
            }
            else
            {
                var step = (ClientRectangle.Width * MillisecondsPerPixels()) / 10.0f;
                
                long newOffset = offset - (int)(e.Delta / SystemInformation.MouseWheelScrollDelta * step);

                if (newOffset < 0)
                    newOffset = 0;

                if (newOffset + ClientRectangle.Width * MillisecondsPerPixels() > Length)
                    newOffset = Length - (long)(ClientRectangle.Width * MillisecondsPerPixels());

                this.offset = newOffset;
                Refresh();
            }
        }

        private void VideoCutterTimeline_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.FillRectangle(brushBackground, ClientRectangle);

            TimelineTooltip timelineTooltip = null;

            var infoAreaHeight = 30;
            var infoAreaRect = new Rectangle(ClientRectangle.X, ClientRectangle.Y, ClientRectangle.Width, infoAreaHeight);
            e.Graphics.FillRectangle(brushBackgroundInfoArea, infoAreaRect);

            if (Length != 0)
            {
                var time = TimeSpan.FromMilliseconds(Position);
                var text = string.Format($"Time: {time:hh\\:mm\\:ss\\:fff} ");

                if (HoverPosition != null)
                {
                    var hoverTime = TimeSpan.FromMilliseconds(HoverPosition.Value);
                    text = text + string.Format($" Hovered time: {hoverTime:hh\\:mm\\:ss\\:fff} ");
                }
                PaintStringInBox(e.Graphics, null, Brushes.LightGray, text, infoAreaRect, 10);
            }


            e.Graphics.TranslateTransform(0, infoAreaHeight);

            int timeLineHeight = ClientRectangle.Height - infoAreaHeight;

            if (Length != 0)
            {
                float pixelsPerSecond = PixelsPerMilliseconds() * 1000.0f; 

                if (pixelsPerSecond > 3)
                {
                    for (long position = 0; position <= Length; position += 1000)
                    {
                        var posXPixel = (position - offset) * PixelsPerMilliseconds();
                        if (posXPixel >= -ClientRectangle.Width && posXPixel <= ClientRectangle.Width)
                        {
                            e.Graphics.DrawLine(penTick, (int)posXPixel, 0, (int)posXPixel, timeLineHeight / 4);
                            if (pixelsPerSecond > 30)
                            {
                                var time = TimeSpan.FromMilliseconds(position);
                                var text = string.Format($"{time:hh\\:mm\\:ss\\:fff}");
                                var rect = new Rectangle((int)posXPixel, 0, 100, 15);
                                e.Graphics.DrawString(text, this.Font, Brushes.SlateGray, rect, StringFormat.GenericDefault);
                            }
                        }

                    }
                }


                if (SelectionStart != null && SelectionEnd != null)
                {
                    var pixelsStart = PositionToPixel((long?)SelectionStart.Value);
                    var pixelsEnd = PositionToPixel((long?)SelectionEnd.Value);
                    var selectionRect = new Rectangle(pixelsStart, 0, pixelsEnd - pixelsStart, timeLineHeight);
                    e.Graphics.FillRectangle(brushBackgroundSelected, selectionRect);
                }

                if (SelectionStart != null)
                {
                    var pixel = PositionToPixel(SelectionStart.Value);
                    e.Graphics.FillRectangle(brushSelectionMarker, pixel, 0, 2, timeLineHeight);
                    PaintUpperHalfTriangle(e.Graphics, brushSelectionMarker, pixel, 8, 8, true);
                    PaintBottomHalfTriangle(e.Graphics, brushSelectionMarker, pixel, 8, 8, true, timeLineHeight);
                }
                if (SelectionEnd != null)
                {
                    var pixel = PositionToPixel(SelectionEnd.Value);
                    e.Graphics.FillRectangle(brushSelectionMarker, pixel, 0, 2, timeLineHeight);
                    PaintUpperHalfTriangle(e.Graphics, brushSelectionMarker, pixel, 8, 8, false);
                    PaintBottomHalfTriangle(e.Graphics, brushSelectionMarker, pixel, 8, 8, false, timeLineHeight);
                }

                var positionPixel = PositionToPixel(Position);
                PaintTriangle(e.Graphics, brushPosition, positionPixel, 8, 8);
                e.Graphics.FillRectangle(brushPosition, positionPixel, 0, 1, timeLineHeight);



                if (HoverPosition != null)
                {
                    var pixel = PositionToPixel(HoverPosition);

                    if (selectionStartMoveController.IsDragStartPossible(pixel) || selectionStartMoveController.IsDragInProgress())
                    {
                        timelineTooltip = new TimelineTooltip() { X = pixel, Text = "move clip start" };
                    }
                    if (selectionEndMoveController.IsDragStartPossible(pixel) || selectionEndMoveController.IsDragInProgress())
                    {
                        timelineTooltip = new TimelineTooltip() { X = pixel, Text = "move clip end" };
                    }



                    e.Graphics.FillRectangle(brushHoverPosition, pixel, 0, 1, timeLineHeight);
                    PaintTriangle(e.Graphics, brushHoverPosition, PositionToPixel(HoverPosition), 8, 8);

                    if (SelectionStart == null)
                    {
                        timelineTooltip = new TimelineTooltip() { X = pixel, Text = "middle click to set clip start here" };
                    }
                    else if (SelectionEnd == null)
                    {
                        timelineTooltip = new TimelineTooltip() { X = pixel, Text = "middle click to set clip end here" };
                    }
                }


                e.Graphics.ResetTransform();


                if (timelineTooltip != null)
                {
                    PaintStringInBox(e.Graphics, Brushes.LightYellow, Brushes.Gray, timelineTooltip.Text, infoAreaRect, timelineTooltip.X);
                }

            }
        }

        private void PaintStringInBox(Graphics gr, Brush background, Brush textBrush, string str, Rectangle parentRectangle, int location)
        {
            var font = this.Font;
            var strSize = gr.MeasureString(str, font);

            var tmpRect = new RectangleF(location, 0f, strSize.Width, strSize.Height);
            tmpRect.Inflate(2, 2);

            var rect = new RectangleF(Math.Max(0, tmpRect.X - tmpRect.Width / 2.0f), tmpRect.Y + (parentRectangle.Height - strSize.Height)/2.0f, tmpRect.Width, tmpRect.Height);
            if (background != null)
                gr.FillRectangle(background, rect);
            
            var stringFormat = new StringFormat();
            stringFormat.Alignment = StringAlignment.Center;
            stringFormat.LineAlignment = StringAlignment.Center;
            gr.DrawString(str, font, textBrush, rect, stringFormat);
        }

        private void PaintTriangle(Graphics gr, Brush brush, int location, int width, int height)
        {
            gr.FillPolygon(brush, new PointF[]
            {
                new PointF(location - width/2.0f, 0),
                new PointF(location + width/2.0f, 0),
                new PointF(location, height)
            });
        }


        private void PaintUpperHalfTriangle(Graphics gr, Brush brush, int location, int width, int height, bool forward)
        {
            gr.FillPolygon(brush, new PointF[]
            {
                new PointF(location, 0),
                new PointF(forward ? location + width : location-width, 0),
                new PointF(location, height)
            });
        }

        private void PaintBottomHalfTriangle(Graphics gr, Brush brush, int location, int width, int height, bool forward, int offsetY)
        {
            gr.FillPolygon(brush, new PointF[]
            {
                new PointF(location, offsetY),
                new PointF(forward ? location + width : location-width, offsetY),
                new PointF(location, offsetY-height)
            });
        }

        private float PixelsPerMilliseconds()
        {
            return ((float)ClientRectangle.Width / Length) * scale;
        }
        private float MillisecondsPerPixels()
        {
            return ((float)Length / ClientRectangle.Width) / scale;
        }


        private int PositionToPixel(long? position)
        {
            if (position == null)
                return 0;

            if (Length == 0)
                return 0;

            return (int)((position.Value - offset) * PixelsPerMilliseconds());
        }

        private long PixelToPosition(float x)
        {
            if (Length == 0)
                return 0;
            return (long)(offset + x * MillisecondsPerPixels());
        }

        private void VideoCutterTimeline_Resize(object sender, EventArgs e)
        {
            Invalidate();
        }


        private void VideoCutterTimeline_MouseMove(object sender, MouseEventArgs e)
        {
            HoverPosition = PixelToPosition(e.Location.X);

            Cursor = Cursors.Default;
            
            selectionStartMoveController.ProcessMouseMove(e);
            selectionEndMoveController.ProcessMouseMove(e);

        }

        private void VideoCutterTimeline_MouseLeave(object sender, EventArgs e)
        {
            HoverPosition = null;
            
            selectionStartMoveController.ProcessMouseLeave(e);
            selectionEndMoveController.ProcessMouseLeave(e);

            Cursor = Cursors.Default;
        }

        private void OnSelectionChanged()
        {
            SelectionChanged?.Invoke(this, new SelectionChangedEventArgs());
        }


        private void OnPositionChangeRequest(long frame)
        {
            PositionChangeRequest?.Invoke(this, new PositionChangeRequestEventArgs() { Position = frame });
        }

        /// <summary>
        /// Creates/updates/clears selection. 
        /// Once selection is changed, the 'SelectionChanged' event is raised. 
        /// </summary>
        public void SetSelection(long? selectionStart, long? selectionEnd)
        {
            if ((selectionStart == null && selectionEnd != null) || (selectionStart != null && selectionEnd != null && selectionStart.Value >= selectionEnd.Value))
                return;

            if ((selectionStart == null && selectionEnd  != null) || (selectionEnd != null && selectionEnd.Value <= selectionStart))
                return;

            this.selectionStart = selectionStart;
            this.selectionEnd = selectionEnd;

            Invalidate();

            OnSelectionChanged();
        }


        private void VideoCutterTimeline_MouseDown(object sender, MouseEventArgs e)
        {
            selectionStartMoveController.ProcessMouseDown(e);
            selectionEndMoveController.ProcessMouseDown(e);
        }

        private void VideoCutterTimeline_MouseUp(object sender, MouseEventArgs e)
        {
            if (!selectionStartMoveController.IsDragInProgress() && !selectionEndMoveController.IsDragInProgress())
            {
                var frame = PixelToPosition(e.X);
                if (e.Button == MouseButtons.Middle && e.Clicks == 1)
                {
                    if (SelectionStart == null)
                    {
                        SetSelection(frame, null);
                    }
                    else if (SelectionEnd == null)
                    {
                        SetSelection(SelectionStart.Value, frame);
                    }
                }
                else if (e.Button == MouseButtons.Left && e.Clicks == 1)
                {

                    OnPositionChangeRequest(frame);
                }
            }
            else
            {
                selectionStartMoveController.ProcessMouseUp(e);
                selectionEndMoveController.ProcessMouseUp(e);
            }
        }

        private abstract class PositionMoveController
        {
            protected VideoCutterTimeline ctrl;
            protected bool dragInProgress = false; 
            
            public PositionMoveController(VideoCutterTimeline ctrl)
            {
                this.ctrl = ctrl;
            }

            protected abstract long? GetCurrentPosition();
            protected abstract void SetCurrentPosition(long frame);

            public bool IsDragInProgress()
            {
                return dragInProgress;
            }

            public bool IsDragStartPossible(int posX)
            {
                return !dragInProgress && IsInDragSizeByFrame(posX, GetCurrentPosition());
            }

            public void ProcessMouseMove(MouseEventArgs e)
            {
                if (dragInProgress)
                {
                    ctrl.Cursor = Cursors.SizeWE;
                    var newPos = ctrl.PixelToPosition(e.X);
                    SetCurrentPosition(newPos);
                }
                else
                {
                    if (IsInDragSizeByFrame(e.X, GetCurrentPosition()))
                    {
                        ctrl.Cursor = Cursors.SizeWE;
                    }
                }
            }
            
            public void ProcessMouseLeave(EventArgs e)
            {
                dragInProgress = false;
            }
            public void ProcessMouseDown(MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left && e.Clicks == 1)
                {
                    if (IsInDragSizeByFrame(e.X, GetCurrentPosition()))
                    {
                        dragInProgress = true; 
                    }
                }
            }

            public void ProcessMouseUp(MouseEventArgs e)
            {
                if (dragInProgress)
                {
                    dragInProgress = false;
                    var frame = GetCurrentPosition().Value;
                    ctrl.OnPositionChangeRequest(frame);
                }
            }


            private bool IsInDragSizeByFrame(int testedX, long? refFrame)
            {
                if (refFrame == null)
                    return false;
                var refX = ctrl.PositionToPixel(refFrame.Value);
                return Math.Abs(testedX - refX) < SystemInformation.DragSize.Width;
            }
        }
        private class SelectionStartMoveController : PositionMoveController
        {
            public SelectionStartMoveController(VideoCutterTimeline ctrl) : base(ctrl)
            {
            }

            protected override long? GetCurrentPosition()
            {
                return ctrl.SelectionStart;
            }

            protected override void SetCurrentPosition(long frame)
            {
                if (ctrl.SelectionEnd == null || ctrl.SelectionEnd > frame  + 1)
                    ctrl.SetSelection(frame, ctrl.selectionEnd);
            }
        }
        private class SelectionEndMoveController : PositionMoveController
        {
            public SelectionEndMoveController(VideoCutterTimeline ctrl) : base(ctrl)
            {
            }

            protected override long? GetCurrentPosition()
            {
                return ctrl.SelectionEnd;
            }
            protected override void SetCurrentPosition(long frame)
            {
                if (ctrl.SelectionStart != null && frame > ctrl.SelectionStart + 1)
                    ctrl.SetSelection(ctrl.selectionStart, frame);
            }
        }

        private class TimelineTooltip
        {
            public int X { get; set; }
            public string Text { get; set; }
        }
    }


    public class TimelineHoverEventArgs : EventArgs
    {
    }


    public class SelectionChangedEventArgs : EventArgs
    {
    }

    public class PositionChangeRequestEventArgs : EventArgs
    {
        public long Position { get; set; }
    }
}
