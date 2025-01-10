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
using System.Security.Claims;

namespace SpiderGH
{
    public class S_Solver
    {

        #region SOLVER METHODS

        public static void OneIteration(List<S_Node> allNodes, List<S_Chain> allChains, bool parallel,
                                        Vector3d gravity, double friction, int substeps, double maxAmplitude,
                                        bool updateMass)
        {

            if (!parallel)
            {
                for (int i = 0; i < allNodes.Count; i++)
                    allNodes[i].CalculateForces(gravity, friction, substeps, maxAmplitude);

                for (int i = 0; i < allNodes.Count; i++)
                    allNodes[i].UpdatePosition();
            }
            else
            {
                Parallel.For(0, allNodes.Count, i =>
                {
                    allNodes[i].CalculateForces(gravity, friction, substeps, maxAmplitude);
                });

                Parallel.For(0, allNodes.Count, i =>
                {
                    allNodes[i].UpdatePosition();
                });
            }

            foreach (var chain in allChains)
                chain.UpdatePolyLength();

            if (updateMass)
            {
                foreach (var chain in allChains)
                    chain.ResetChainMassToZero();

                foreach (var chain in allChains)
                    chain.UpdateChainMass();
            }
        }


        #endregion


        #region POINT METHODS


        public static bool RemoveDuplicatePoints(Point3dList inputPoints, double minDistance, out Point3dList outputPoints)
        {
            #region Test input data

            outputPoints = null;

            if (inputPoints == null || inputPoints.Count < 1)
                return false;

            if (double.IsNaN(minDistance))
                return false;

            foreach (Point3d pt in inputPoints)
                if (pt == Point3d.Unset || !pt.IsValid)
                    return false;

            #endregion

            outputPoints = new Point3dList();
            outputPoints.Add(inputPoints[0]);

            double minDistSquared = minDistance * minDistance * 1.5;

            for (int i = 1; i < inputPoints.Count; i++)
            {
                Point3d testPt = inputPoints[i];

                bool addPt = true; //assume to add a point
                foreach (Point3d acceptedPt in outputPoints)
                {
                    if (testPt.DistanceToSquared(acceptedPt) < minDistSquared) //first fast check for distance tolerance
                    {
                        if (testPt.DistanceTo(acceptedPt) < minDistance) //second real check for distance tolerance
                        {
                            addPt = false; //the point within the tolerance already exists
                            break; //no need for futher tetsing
                        }
                    }
                }

                if (addPt)
                    outputPoints.Add(testPt);
            }

            return true;
        }

        public static bool IsPointOnCurve(Point3d pt, Curve crv, double tolerance)
        {


            if (pt == Point3d.Unset || !pt.IsValid)
                return false;

            if (crv == null || !crv.IsValid || crv.IsShort(Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance))
                return false;

            bool test = crv.ClosestPoint(pt, out double t);

            if (!test)
                return false;

            double distance = pt.DistanceTo(crv.PointAt(t));

            if (distance < tolerance)
                return true;
            else
                return false;
        }

        public static bool IsPointOnCurves(Point3d pt, List<Curve> crvs, double tolerance)
        {
            bool onCurves = false;

            foreach (var crv in crvs)
            {
                bool test = IsPointOnCurve(pt, crv, tolerance);

                if (test)
                {
                    onCurves = true;
                    break;
                }
            }

            return onCurves;

        }

        public static bool PointOnClosestCurves(Point3d pt, List<Curve> crvs,
            out Point3d closestPt, out int curveIndex, out double distance)
        {
            closestPt = Point3d.Unset;
            curveIndex = -1;
            distance = double.NaN;

            if (pt == Point3d.Unset || !pt.IsValid)
                return false;

            if (crvs == null || crvs.Count < 1)
                return false;

            distance = 1e12;

            for (int i = 0; i < crvs.Count; i++)
            {
                crvs[i].ClosestPoint(pt, out double t);
                Point3d testPt = crvs[i].PointAt(t);

                if (pt.DistanceTo(testPt) < distance)
                {
                    distance = pt.DistanceTo(testPt);
                    closestPt = testPt;
                    curveIndex = i;
                }
            }

            return true;
        }

        public static bool PointsOnClosestCurves(Point3dList pts, List<Curve> crvs,
            out List<Point3d> closestPts, out List<int> curveIndices, out List<double> distances)
        {
            closestPts = new List<Point3d>();
            curveIndices = new List<int>();
            distances = new List<double>();

            if (pts == null || pts.Count < 1)
                return false;

            if (crvs == null || crvs.Count < 1)
                return false;

            int falseCounter = 0;

            for (int i = 0; i < pts.Count; i++)
            {
                bool test = PointOnClosestCurves(pts[i], crvs,
                    out Point3d closestPt, out int curveIndex, out double distance);

                if (!test)
                    falseCounter++;

                closestPts.Add(closestPt);
                curveIndices.Add(curveIndex);
                distances.Add(distance);
            }

            if (falseCounter > 0)
                return false;

            return true;
        }


        #endregion


        #region LINE METHODS

        public static Line FlipLineDirection(Line inputLine, Point3d guidePt, bool towardsPoint, out bool flipResult)
        {
            flipResult = false;

            Line tempLine = Line.Unset;

            double distStart = guidePt.DistanceTo(inputLine.From);
            double distEnd = guidePt.DistanceTo(inputLine.To);

            if (towardsPoint)
            {
                //if distance to start is smaller than distance to end, we need to flip
                if (distStart < distEnd)
                {
                    tempLine = new Line(inputLine.To, inputLine.From); //flipped
                    flipResult = true;
                }
                else
                {
                    tempLine = new Line(inputLine.From, inputLine.To); //regular
                    flipResult = false;
                }
            }
            else
            {
                //if distance to start is larger than distance to end, we need to flip
                if (distStart > distEnd)
                {
                    tempLine = new Line(inputLine.To, inputLine.From); //flipped
                    flipResult = true;
                }
                else
                {
                    tempLine = new Line(inputLine.From, inputLine.To); //regular
                    flipResult = false;
                }
            }

            return tempLine;
        }

        public static List<Line> FlipLineDirection(List<Line> inputLines, Point3d guidePt, bool towardsPoint, out List<bool> flipResult)
        {
            #region test input values

            flipResult = null;

            if (inputLines == null || inputLines.Count == 0)
                return null;

            if (guidePt == Point3d.Unset || !guidePt.IsValid)
                return null;

            foreach (Line line in inputLines)
            {
                if (line == Line.Unset || !line.IsValid)
                    return null;
            }

            #endregion

            flipResult = new List<bool>();
            List<Line> resLines = new List<Line>();


            foreach (Line iLine in inputLines)
            {
                bool flip = false;
                Line nLine = FlipLineDirection(iLine, guidePt, towardsPoint, out flip);

                resLines.Add(nLine);
                flipResult.Add(flip);
            }

            return resLines;
        }

        public static Line FlipLineDirection(Line inputLine, Line guideLn, out bool flipResult)
        {
            Line tempLine = Line.Unset;

            if (RhinoMath.ToDegrees(Math.Abs(Vector3d.VectorAngle(guideLn.Direction, inputLine.Direction))) > 90)
            {
                //flip the line
                tempLine = new Line(inputLine.To, inputLine.From);
                flipResult = true;
            }
            else
            {
                //keep the line
                tempLine = new Line(inputLine.From, inputLine.To);
                flipResult = false;
            }

            return tempLine;
        }

        public static List<Line> FlipLineDirection(List<Line> inputLines, Line guideLn, out List<bool> flipResult)
        {
            #region test input values

            flipResult = null;

            if (inputLines == null || inputLines.Count == 0)
                return null;

            foreach (Line line in inputLines)
            {
                if (line == Line.Unset || !line.IsValid)
                    return null;
            }

            if (guideLn == Line.Unset || !guideLn.IsValid)
                return null;

            #endregion

            flipResult = new List<bool>();
            List<Line> resLines = new List<Line>();

            foreach (Line iLine in inputLines)
            {
                bool flip = false;
                Line nLine = FlipLineDirection(iLine, guideLn, out flip);

                resLines.Add(nLine);
                flipResult.Add(flip);
            }

            return resLines;
        }

        public static Line FlipLineDirection(Line inputLine, Vector3d guideVec, out bool flipResult)
        {
            Line tempLine = Line.Unset;

            if (RhinoMath.ToDegrees(Math.Abs(Vector3d.VectorAngle(guideVec, inputLine.Direction))) > 90)
            {
                //flip the line
                tempLine = new Line(inputLine.To, inputLine.From);
                flipResult = true;
            }
            else
            {
                //keep the line
                tempLine = new Line(inputLine.From, inputLine.To);
                flipResult = false;
            }

            return tempLine;
        }

        public static List<Line> FlipLineDirection(List<Line> inputLines, Vector3d guideVec, out List<bool> flipResult)
        {
            #region test input values

            flipResult = null;

            if (inputLines == null || inputLines.Count == 0)
                return null;

            foreach (Line line in inputLines)
            {
                if (line == Line.Unset || !line.IsValid)
                    return null;
            }

            if (guideVec == Vector3d.Unset || !guideVec.IsValid)
                return null;

            #endregion

            flipResult = new List<bool>();
            List<Line> resLines = new List<Line>();

            foreach (Line iLine in inputLines)
            {
                bool flip = false;
                Line nLine = FlipLineDirection(iLine, guideVec, out flip);

                resLines.Add(nLine);
                flipResult.Add(flip);
            }

            return resLines;
        }

        public static Line FlipLineDirection(Line inputLine, Plane guidePln, bool posX, bool posY, bool posZ, out bool flipResult)
        {
            Line tempLine = Line.Unset;
            double x, y, z;
            x = y = z = 1;
            if (!posX) x = -1;
            if (!posY) y = -1;
            if (!posZ) z = -1;

            Point3d ptE = guidePln.PointAt(x, y, z);
            Vector3d direction = ptE - guidePln.Origin;

            if (RhinoMath.ToDegrees(Math.Abs(Vector3d.VectorAngle(direction, inputLine.Direction))) > 90)
            {
                //flip the line
                tempLine = new Line(inputLine.To, inputLine.From);
                flipResult = true;
            }
            else
            {
                //keep the line
                tempLine = new Line(inputLine.From, inputLine.To);
                flipResult = false;
            }

            return tempLine;
        }

        public static List<Line> FlipLineDirection(List<Line> inputLines, Plane guidePln, bool posX, bool posY, bool posZ, out List<bool> flipResult)
        {
            #region test input values

            flipResult = null;

            if (inputLines == null || inputLines.Count == 0)
                return null;

            foreach (Line line in inputLines)
            {
                if (line == Line.Unset || !line.IsValid)
                    return null;
            }

            if (guidePln == Plane.Unset || !guidePln.IsValid)
                return null;

            #endregion

            flipResult = new List<bool>();
            List<Line> resLines = new List<Line>();

            foreach (Line iLine in inputLines)
            {
                bool flip = false;
                Line nLine = FlipLineDirection(iLine, guidePln, posX, posY, posZ, out flip);

                resLines.Add(nLine);
                flipResult.Add(flip);
            }

            return resLines;
        }


        #endregion

        #region CURVE METHODS

        public static bool SplitLineCurve(LineCurve lncrv, Point3dList pts, double searchDist, double endDist,
                                          out List<LineCurve> segments)
        {
            double tolerance = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
            segments = null;

            //testing input values

            if (lncrv == null || !lncrv.IsValid || lncrv.IsShort(Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance))
            {
                return false;
            }
            else if (pts == null || pts.Count < 1)
            {
                return false;
            }

            if (searchDist < 0)
                searchDist = Math.Abs(searchDist);

            if (endDist < 0)
                endDist = Math.Abs(endDist);

            //finding closest points
            segments = new List<LineCurve>();

            Point3dList closepts = new Point3dList();
            List<double> closet = new List<double>();
            foreach (var pt in pts)
            {
                bool test = lncrv.ClosestPoint(pt, out double t, searchDist);

                if (test)
                {
                    Point3d testPt = lncrv.PointAt(t);
                    if (testPt.DistanceTo(lncrv.Line.From) > endDist &&
                        testPt.DistanceTo(lncrv.Line.To) > endDist)
                    {
                        closepts.Add(testPt);
                        closet.Add(t);
                    }
                }
            }

            //no need to split the curve
            if (closepts.Count < 1)
            {
                segments.Add(new LineCurve(lncrv.Line));
                return true;
            }

            Curve[] crvs = lncrv.Split(closet);

            if (crvs != null && crvs.Length > 0)
            {
                foreach (var crv in crvs)
                    if (crv != null && crv.IsValid && !crv.IsShort(tolerance))
                        segments.Add(new LineCurve(new Line(crv.PointAtStart, crv.PointAtEnd)));

                return true;
            }
            else
            {
                segments = null;
                return false;
            }
        }


        public static bool ConvertCurveToPolyline(Curve inputCrv, double resolution, bool detectSegments, out Polyline poly)
        {
            #region TEST INPUT DATA

            poly = null;

            if (inputCrv == null || !inputCrv.IsValid)
                return false;

            if (double.IsNaN(resolution))
                return false;

            if (resolution < 0)
                resolution = Math.Abs(resolution);

            #endregion

            poly = new Polyline();
            double length = inputCrv.GetLength();
            int n = Convert.ToInt32(length / resolution);

            if (n < 2) n = 2;
            if (n > 10000) n = 10000;

            poly.Add(inputCrv.PointAtStart);

            if (detectSegments)
            {
                Curve[] segs = inputCrv.DuplicateSegments();

                foreach (Curve segment in segs)
                {
                    int xn = Convert.ToInt32((double)n * segment.GetLength() / length);

                    for (int i = 0; i < xn; i++)
                    {
                        double t = (double)(i + 1) / (double)xn;
                        poly.Add(segment.PointAtNormalizedLength(t));
                    }
                }

            }
            else
            {
                for (int i = 0; i < n; i++)
                {
                    double t = (double)(i + 1) / (double)n;
                    poly.Add(inputCrv.PointAtNormalizedLength(t));
                }
            }

            return true;
        }


        #endregion

        #region SHAPES

        public static bool ConstructRectangle(Plane plane, double a, double b, out Polyline rectangle)
        {
            rectangle = null;

            if (plane == Plane.Unset)
                return false;

            if (a < 0)
                a = Math.Abs(a);
            if (a < Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance * 20)
                a = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance * 20;

            if (b < 0)
                b = Math.Abs(b);
            if (b < Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance * 20)
                b = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance * 20;

            rectangle = new Polyline();
            rectangle.Add(plane.Origin + plane.XAxis * a * 0.5 + plane.YAxis * b * 0.5);
            rectangle.Add(plane.Origin - plane.XAxis * a * 0.5 + plane.YAxis * b * 0.5);
            rectangle.Add(plane.Origin - plane.XAxis * a * 0.5 - plane.YAxis * b * 0.5);
            rectangle.Add(plane.Origin + plane.XAxis * a * 0.5 - plane.YAxis * b * 0.5);
            rectangle.Add(rectangle[0]);

            if (!rectangle.IsValid || rectangle.Length < Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance * 10)
                return false;

            return true;
        }

        /// <summary>
        /// TYPE: 0 = radius, 1 = inner radius, 2 = edge length
        /// </summary>
        /// <param name="plane"></param>
        /// <param name="sides"></param>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <param name="closed"></param>
        /// <param name="polygon"></param>
        /// <returns></returns>
        public static bool ConstructRegularPolygon(Plane plane, int sides, int type, double value, bool closed, out Polyline polygon)
        {
            #region TESTING

            polygon = null;

            double minValue = 1e-6;

            if (plane == Plane.Unset || !plane.IsValid)
                return false;

            if (sides < 0) sides = Math.Abs(sides);
            if (sides < 3) sides = 3;

            if (value < 0) value = Math.Abs(value);
            if (value < minValue) value = minValue;

            if (type < 0) type = Math.Abs(type);
            if (type > 2) type = 2;

            #endregion

            #region COMPUTING VALUES

            // TYPE: 0 = radius, 1 = inner radius, 2 = edge length

            double radius, innerRadius, edgeLength;

            double piOverSides = Math.PI / sides;

            if (type == 0)
            {
                radius = value;
                // - - - - - - - - - 
                innerRadius = radius * Math.Cos(piOverSides);
                edgeLength = radius * 2 * Math.Sin(piOverSides);

            }
            else if (type == 1)
            {
                innerRadius = value;
                // - - - - - - - - - 
                radius = innerRadius / (Math.Cos(piOverSides));
                edgeLength = radius * 2 * Math.Sin(piOverSides);
            }
            else
            {
                edgeLength = value;
                // - - - - - - - - - 
                radius = edgeLength / (2 * Math.Sin(piOverSides));
                innerRadius = radius * Math.Cos(piOverSides);
            }

            #endregion

            #region ALGORITHM

            polygon = new Polyline();
            polygon = Polyline.CreateInscribedPolygon(new Circle(plane, radius), sides);

            if (polygon == null || !polygon.IsValid)
            {
                polygon = null;
                return false;
            }

            if (!closed)
                polygon.RemoveAt(polygon.Count - 1);

            return true;

            #endregion


        }

        public static bool ConstructStar(Plane plane, int sides, double r1, double r2, out Polyline star)
        {
            star = null;

            if (plane == Plane.Unset)
                return false;

            if (r1 < 0)
                r1 = Math.Abs(r1);
            if (r1 < Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance * 10)
                r1 = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance * 10;

            if (r2 < 0)
                r2 = Math.Abs(r2);
            if (r2 < Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance * 10)
                r2 = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance * 10;

            if (sides < 0)
                sides = Math.Abs(sides);
            if (sides < 3)
                sides = 3;
            if (sides > 100)
                sides = 100;

            bool test = ConstructRegularPolygon(plane, sides, 0, r1, true, out Polyline polygon);

            if (!test || polygon == null || !polygon.IsValid || polygon.Length < Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance * 10)
                return false;

            star = new Polyline();
            for (int i = 0; i < polygon.Count - 1; i++)
            {
                star.Add(new Point3d(polygon[i]));

                Point3d ptMid = (polygon[i] + polygon[i + 1]) * 0.5;
                Vector3d vecMid = new Vector3d(ptMid - plane.Origin);
                vecMid.Unitize();

                Point3d ptNew = plane.Origin + vecMid * r2;
                star.Add(ptNew);
            }

            if (star.IsClosed == false)
                star.Add(star[0]);

            if (!star.IsValid || star.Length < Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance * 10)
                return false;

            return true;
        }

        public static bool ConstructPolyEllipse(Plane plane, int sides, double r1, double r2, out Polyline ellipse)
        {
            ellipse = null;

            if (plane == Plane.Unset)
                return false;

            if (r1 < 0)
                r1 = Math.Abs(r1);
            if (r1 < Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance * 10)
                r1 = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance * 10;

            if (r2 < 0)
                r2 = Math.Abs(r2);
            if (r2 < Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance * 10)
                r2 = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance * 10;

            if (sides < 0)
                sides = Math.Abs(sides);
            if (sides < 3)
                sides = 3;
            if (sides > 100)
                sides = 100;

            Ellipse regEllipse = new Ellipse(plane, r1, r2);

            if (!regEllipse.IsValid)
                return false;

            Curve crv = regEllipse.ToNurbsCurve();
            crv.DivideByCount(sides, true, out Point3d[] pts);

            if (pts == null || pts.Length < 3)
                return false;

            ellipse = new Polyline();
            ellipse.AddRange(pts);

            if (ellipse.IsClosed == false)
                ellipse.Add(ellipse[0]);

            if (!ellipse.IsValid || ellipse.Length < Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance * 10)
                return false;

            return true;
        }



        #endregion


        #region MESH METHODS

        public static bool SimplePolylineToMesh(Polyline poly, Point3d center, out Mesh mesh)
        {
            mesh = null;

            if (poly == null || !poly.IsValid || poly.Count < 3) //|| !poly.IsClosed
                return false;

            mesh = new Mesh();
            mesh.Vertices.Add(center); //0
            mesh.Vertices.AddVertices(poly); //rest

            for (int i = 1; i < poly.Count; i++) //starting with index 1! 0 = center point
            {
                mesh.Faces.AddFace(0, i, i + 1);
            }

            mesh.Weld(RhinoMath.ToRadians(45));
            mesh.Normals.ComputeNormals();
            mesh.UnifyNormals();
            mesh.Compact();

            if (mesh == null || !mesh.IsValid || mesh.Vertices.Count < 1)
                return false;

            return true;
        }

        public static bool GetMeshEdges(Mesh mesh, out List<Line> edges)
        {
            edges = null;

            if (mesh == null || !mesh.IsValid || mesh.Vertices.Count < 1 || mesh.Faces.Count < 1)
                return false;

            edges = new List<Line>();

            MeshTopologyEdgeList topoedge = mesh.TopologyEdges;

            for (int i = 0; i < topoedge.Count; i++)
                edges.Add(topoedge.EdgeLine(i));

            if (edges.Count < 1)
                return false;

            return true;
        }

        public static bool SingleSubdivideMesh(Mesh meshS, out Mesh meshE)
        {
            meshE = null;

            if (meshS == null || !meshS.IsValid || meshS.Vertices.Count < 1 || meshS.Faces.Count < 1)
                return false;

            MeshFaceList faces = meshS.Faces;
            MeshVertexList vertices = meshS.Vertices;

            meshE = new Mesh();

            for (int i = 0; i < faces.Count; i++)
            {
                MeshFace face = faces[i];
                int mli = meshE.Vertices.Count; //last mesh index

                if (face.IsQuad)
                {
                    Point3d ptA = new Point3d(vertices[face.A]); //+0
                    Point3d ptB = new Point3d(vertices[face.B]); //+1
                    Point3d ptC = new Point3d(vertices[face.C]); //+2
                    Point3d ptD = new Point3d(vertices[face.D]); //+3

                    Point3d ptAB = (ptA + ptB) * 0.5; //+4
                    Point3d ptBC = (ptB + ptC) * 0.5; //+5
                    Point3d ptCD = (ptC + ptD) * 0.5; //+6
                    Point3d ptDA = (ptD + ptA) * 0.5; //+7

                    Point3d ptO = (ptA + ptB + ptC + ptD) * 0.25; //+8

                    List<Point3d> newVertices = new List<Point3d>() { ptA, ptB, ptC, ptD, ptAB, ptBC, ptCD, ptDA, ptO };
                    meshE.Vertices.AddVertices(newVertices);

                    meshE.Faces.AddFace(mli + 0, mli + 4, mli + 8, mli + 7);
                    meshE.Faces.AddFace(mli + 1, mli + 5, mli + 8, mli + 4);
                    meshE.Faces.AddFace(mli + 2, mli + 6, mli + 8, mli + 5);
                    meshE.Faces.AddFace(mli + 3, mli + 7, mli + 8, mli + 6);
                }
                else if (face.IsTriangle)
                {
                    Point3d ptA = new Point3d(vertices[face.A]); //+0
                    Point3d ptB = new Point3d(vertices[face.B]); //+1
                    Point3d ptC = new Point3d(vertices[face.C]); //+2

                    Point3d ptAB = (ptA + ptB) * 0.5; //+3
                    Point3d ptBC = (ptB + ptC) * 0.5; //+4
                    Point3d ptCA = (ptC + ptA) * 0.5; //+5

                    Point3d ptO = (ptA + ptB + ptC) * 0.3333; //+6

                    List<Point3d> newVertices = new List<Point3d>() { ptA, ptB, ptC, ptAB, ptBC, ptCA, ptO };
                    meshE.Vertices.AddVertices(newVertices);

                    meshE.Faces.AddFace(mli + 0, mli + 3, mli + 6, mli + 5);
                    meshE.Faces.AddFace(mli + 1, mli + 4, mli + 6, mli + 3);
                    meshE.Faces.AddFace(mli + 2, mli + 5, mli + 6, mli + 4);
                }
            }

            meshE.Normals.ComputeNormals();
            meshE.Weld(RhinoMath.ToRadians(45));
            meshE.Normals.ComputeNormals();

            if (meshE == null || !meshE.IsValid || meshE.Vertices.Count < 1)
                return false;

            return true;
        }


        #endregion

        #region NUMBERS

        public static double RemapNumber(double inputNumber, Interval source, Interval target)
        {
            double number2 = ((inputNumber - source[0]) * (target[1] - target[0]) / (source[1] - source[0])) + target[0];
            return number2;
        }


        #endregion


    }
}
