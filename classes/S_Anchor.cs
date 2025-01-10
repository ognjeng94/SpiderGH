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
    public class S_Anchor
    {
        #region PROPERTIES

        public bool IsValid { get; set; }

        public Point3d Position { get; set; }

        public double DistanceTolerance { get; set; }

        public bool MoveX { get; set; }
        public bool MoveY { get; set; }
        public bool MoveZ { get; set; }

        #endregion

        #region CONSTRUCTOR

        ///this construct will be used only to pass the data
        public S_Anchor(Point3d pt, double distTolerance, bool x, bool y, bool z)
        {
            if (distTolerance < 0)
                distTolerance = Math.Abs(distTolerance);

            IsValid = true;

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

            MoveX = x;
            MoveY = y;
            MoveZ = z;
        }

        #endregion

        #region METHODS



        #endregion
    }
}
