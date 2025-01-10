using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

using Rhino;
using Rhino.Collections;
using Rhino.Input.Custom;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Geometry.Collections;
using Rhino.DocObjects;
using Rhino.Geometry.Intersect;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

namespace SpiderGH
{
    public class S_Load
    {
        #region PROPERTIES

        public enum LoadType
        {
            PointLoad = 0,
            LineLoad,
            BrepLoad
        }

        public LoadType Type { get; set; }
        public bool IsValid { get; set; }

        public Point3d Position { get; set; }
        public LineCurve LinePosition { get; set; }
        public Brep BrepPosition { get; set; }

        public double DistanceTolerance { get; set; }
        public double Mass { get; set; }

        #endregion

        #region CONSTRUCTOR


        ///this constructor will be used only to pass the data
        public S_Load(Point3d pt, double distTolerance, double mass)
        {
            Type = LoadType.PointLoad;
            IsValid = true;

            if (mass < 0)
                mass = Math.Abs(mass);

            if (distTolerance < 0)
                distTolerance = Math.Abs(distTolerance);

            if (pt != Point3d.Unset && pt.IsValid)
            {
                Position = new Point3d(pt);
            }
            else
            {
                Position = Point3d.Unset;
                IsValid = false;
            }

            DistanceTolerance = distTolerance;
            Mass = mass;

            LinePosition = null;
            BrepPosition = null;
        }

        ///this constructor will be used only to pass the data
        public S_Load(Line line, double distTolerance, double massPerM)
        {
            Type = LoadType.LineLoad;
            IsValid = true;

            if (massPerM < 0)
                massPerM = Math.Abs(massPerM);

            if (distTolerance < 0)
                distTolerance = Math.Abs(distTolerance);

            if (line.IsValid && Math.Abs(line.Length) > 1e-12)
                {
                LinePosition = new LineCurve(line);
            }
            else
            {
                LinePosition = null;
                IsValid = false;
            }

            DistanceTolerance = distTolerance;
            Mass = massPerM * LinePosition.Line.Length;

            Position = Point3d.Unset;
            BrepPosition = null;
        }

        ///this constructor will be used only to pass the data
        public S_Load(Brep brep, double distTolerance, double mass)
        {
            Type = LoadType.BrepLoad;
            IsValid = true;

            if (mass < 0)
                mass = Math.Abs(mass);

            if (distTolerance < 0)
                distTolerance = Math.Abs(distTolerance);

            if (brep != null && brep.IsValid && brep.IsSolid && brep.GetVolume(1e-2, 1e-3) > 1e-6)
            {
                BrepPosition = brep.DuplicateBrep();
            }
            else
            {
                BrepPosition = null;
                IsValid = false;
            }

            DistanceTolerance = distTolerance;
            Mass = mass;

            Position = Point3d.Unset;
            LinePosition = null;
        }


        #endregion

        #region METHODS



        #endregion
    }
}
