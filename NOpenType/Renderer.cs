﻿//Apache2, 2014-2016,   WinterDev
using System;
using FtPointD = NRasterizer.Point<double>;

namespace NRasterizer
{
    public class Renderer
    {
        private readonly IGlyphRasterizer _rasterizer;
        private readonly Typeface _typeface;
        private const int pointsPerInch = 72;
        private const double FT_RESIZE = 64; //essential to be floating point

        public Renderer(Typeface typeface, IGlyphRasterizer rasterizer)
        {
            _typeface = typeface;
            _rasterizer = rasterizer;
        }

        void RenderGlyph(float x, float y, Glyph glyph)
        {
            var rasterizer = new TranslatingRasterizer(x, y, _rasterizer);

            ushort[] contours = glyph.EndPoints;
            short[] xs = glyph.X;
            short[] ys = glyph.Y;
            bool[] onCurves = glyph.On;

            //outline version
            //-----------------------------
            int npoints = xs.Length;
            int startContour = 0;
            int cpoint_index = 0;
            //-----------------------------------
            rasterizer.BeginRead(contours.Length);
            //-----------------------------------
            double lastMoveX = 0;
            double lastMoveY = 0;

            int controlPointCount = 0;
            for (int i = 0; i < contours.Length; i++)
            {
                int nextContour = contours[startContour] + 1;
                bool isFirstPoint = true;
                FtPointD secondControlPoint = new FtPointD();
                FtPointD thirdControlPoint = new FtPointD();
                bool justFromCurveMode = false;

                for (; cpoint_index < nextContour; ++cpoint_index)
                {

                    short vpoint_x = xs[cpoint_index];
                    short vpoint_y = ys[cpoint_index];
                    if (onCurves[cpoint_index])
                    {
                        //on curve
                        if (justFromCurveMode)
                        {
                            switch (controlPointCount)
                            {
                                case 1:
                                    {
                                        _rasterizer.Curve3(secondControlPoint.x / FT_RESIZE, secondControlPoint.y / FT_RESIZE,
                                            vpoint_x / FT_RESIZE, vpoint_y / FT_RESIZE);
                                    }
                                    break;
                                case 2:
                                    {
                                        rasterizer.Curve4(secondControlPoint.x / FT_RESIZE, secondControlPoint.y / FT_RESIZE,
                                            thirdControlPoint.x / FT_RESIZE, thirdControlPoint.y / FT_RESIZE,
                                            vpoint_x / FT_RESIZE, vpoint_y / FT_RESIZE);
                                    }
                                    break;
                                default:
                                    {
                                        throw new NotSupportedException();
                                    }
                            }
                            controlPointCount = 0;
                            justFromCurveMode = false;
                        }
                        else
                        {
                            if (isFirstPoint)
                            {
                                isFirstPoint = false;
                                _rasterizer.MoveTo(lastMoveX = (vpoint_x / FT_RESIZE), lastMoveY = (vpoint_y / FT_RESIZE));
                            }
                            else
                            {
                                _rasterizer.LineTo(vpoint_x / FT_RESIZE, vpoint_y / FT_RESIZE);
                            }
                        }
                    }
                    else
                    {
                        switch (controlPointCount)
                        {
                            case 0:
                                {
                                    secondControlPoint = new FtPointD(vpoint_x, vpoint_y);
                                }
                                break;
                            case 1:
                                {
                                    //we already have prev second control point
                                    //so auto calculate line to 
                                    //between 2 point
                                    FtPointD mid = GetMidPoint(secondControlPoint, vpoint_x, vpoint_y);
                                    //----------
                                    //generate curve3
                                    rasterizer.Curve3(secondControlPoint.x / FT_RESIZE, secondControlPoint.y / FT_RESIZE,
                                        mid.x / FT_RESIZE, mid.y / FT_RESIZE);
                                    //------------------------
                                    controlPointCount--;
                                    //------------------------
                                    //printf("[%d] bzc2nd,  x: %d,y:%d \n", mm, vpoint.x, vpoint.y);
                                    secondControlPoint = new FtPointD(vpoint_x, vpoint_y);
                                }
                                break;
                            default:
                                {
                                    throw new NotSupportedException("Too many control points");
                                }
                                break;
                        }

                        controlPointCount++;
                        justFromCurveMode = true;
                    }
                }
                //--------
                //close figure
                //if in curve mode
                if (justFromCurveMode)
                {
                    switch (controlPointCount)
                    {
                        case 0: break;
                        case 1:
                            {
                                rasterizer.Curve3(secondControlPoint.x / FT_RESIZE, secondControlPoint.y / FT_RESIZE,
                                    lastMoveX, lastMoveY);
                            }
                            break;
                        case 2:
                            {
                                rasterizer.Curve4(secondControlPoint.x / FT_RESIZE, secondControlPoint.y / FT_RESIZE,
                                    thirdControlPoint.x / FT_RESIZE, thirdControlPoint.y / FT_RESIZE,
                                    lastMoveX, lastMoveY);
                            }
                            break;
                        default:
                            { throw new NotSupportedException("Too many control points"); }
                    }
                    justFromCurveMode = false;
                    controlPointCount = 0;
                }
                rasterizer.CloseFigure();
                //--------                   
                startContour++;
            }
            _rasterizer.EndRead();
        }

        private static FtPointD GetMidPoint(FtPointD v1, int v2x, int v2y)
        {
            return new FtPointD(
                ((double)v1.X + (double)v2x) / 2d,
                ((double)v1.Y + (double)v2y) / 2d);
        }

        private static FtPointD GetMidPoint(FtPointD v1, FtPointD v2)
        {
            return new FtPointD(
                ((double)v1.x + (double)v2.x) / 2d,
                ((double)v1.y + (double)v2.y) / 2d);
        }

        public void Render(int x, int y, string text, int size, int resolution)
        {
            float xx = x;
            float yy = y;
            foreach (var character in text)
            {
                RenderGlyph(xx, yy, _typeface.Lookup(character));
                xx += _typeface.GetAdvanceWidth(character) / (float)FT_RESIZE;
            }
        }
    }

}