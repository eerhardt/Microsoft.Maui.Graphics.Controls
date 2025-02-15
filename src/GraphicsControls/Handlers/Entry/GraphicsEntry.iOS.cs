﻿#nullable disable
using System;
using System.Diagnostics;
using CoreGraphics;
using Microsoft.Maui.Graphics.Platform;
using UIKit;

namespace Microsoft.Maui.Graphics.Controls
{
    public class GraphicsEntry : UITextField, IMixedNativeView
    {
        readonly PlatformCanvas _canvas;
        readonly UITapGestureRecognizer _tapGesture;

        IMixedGraphicsHandler _graphicsControl;
        CGColorSpace _colorSpace;
        IDrawable _drawable;
        CGRect _lastBounds;
   
        public GraphicsEntry()
        {
            _canvas = new PlatformCanvas(() => CGColorSpace.CreateDeviceRGB());

            EdgeInsets = UIEdgeInsets.Zero;
            BorderStyle = UITextBorderStyle.None;
            ClipsToBounds = true;
            BackgroundColor = UIColor.Clear;

            _tapGesture = new UITapGestureRecognizer(OnTap)
            {
                NumberOfTapsRequired = 1
            };
            AddGestureRecognizer(_tapGesture);
        }

        public IMixedGraphicsHandler GraphicsControl
        {
            get => _graphicsControl;
            set => Drawable = _graphicsControl = value;
        }

        public IDrawable Drawable
        {
            get => _drawable;
            set
            {
                _drawable = value;

                if (_drawable != null)
                    SetNeedsDisplay();
            }
        }

        public UIEdgeInsets EdgeInsets { get; set; }

        static readonly string[] DefaultNativeLayers = new[] { nameof(IEntry.Text) };

        public string[] NativeLayers => DefaultNativeLayers;

        public void Invalidate()
        {
            SetNeedsDisplay();
        }

        public override CGRect Bounds
        {
            get => base.Bounds;

            set
            {
                var newBounds = value;

                if (_lastBounds.Width != newBounds.Width || _lastBounds.Height != newBounds.Height)
                {
                    base.Bounds = value;

                    Invalidate();

                    _lastBounds = newBounds;
                }
            }
        }

        public override CGRect TextRect(CGRect forBounds)
        {
            return base.TextRect(InsetRect(forBounds, EdgeInsets));
        }

        public override CGRect EditingRect(CGRect forBounds)
        {
            return base.EditingRect(InsetRect(forBounds, EdgeInsets));
        }

        public static CGRect InsetRect(CGRect rect, UIEdgeInsets insets)
        {
            return new CGRect(
                rect.X + insets.Left,
                rect.Y + insets.Top,
                rect.Width - insets.Left - insets.Right,
                rect.Height - insets.Top - insets.Bottom);
        }

        public void DrawBaseLayer(RectF dirtyRect)
        {
            base.Draw(dirtyRect);
        }

        public override void Draw(CGRect dirtyRect)
        {
            base.Draw(dirtyRect);

            if (_drawable == null)
                return;

            var coreGraphics = UIGraphics.GetCurrentContext();

            if (_colorSpace == null)
                _colorSpace = CGColorSpace.CreateDeviceRGB();

            coreGraphics.SetFillColorSpace(_colorSpace);
            coreGraphics.SetStrokeColorSpace(_colorSpace);

            Draw(coreGraphics, dirtyRect.AsRectangleF());
        }

        void Draw(CGContext coreGraphics, RectF dirtyRect)
        {
            _canvas.Context = coreGraphics;

            try
            {
                _drawable?.Draw(_canvas, dirtyRect);
            }
            catch (Exception exc)
            {
                Debug.WriteLine("An unexpected error occurred rendering the drawing.", exc);
            }
            finally
            {
                _canvas.Context = null;
            }
        }

        void OnTap()
        {
            try
            {
                if (!IsFirstResponder)
                    BecomeFirstResponder();

                var locationInView = _tapGesture.LocationInView(_tapGesture.View);
                PointF interceptPoint = locationInView.AsPointF();
                PointF[] tapPoints = new PointF[] { interceptPoint };
                GraphicsControl?.StartInteraction(tapPoints);
            }
            catch (Exception exc)
            {
                Debug.WriteLine("An unexpected error occured handling a touch event within the control.", exc);
            }
        }
    }
}