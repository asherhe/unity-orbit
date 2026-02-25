using Orbit;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(FollowWorldTransform))]
    public class Trajectory : MonoBehaviour
    {
        private TrajectoryMesh _trajectoryMesh;
        private MeshRenderer _meshRenderer;
        private FollowWorldTransform _follow;

        private OrbitState _orbit;
        private UniversalPropagator _prop;
        public OrbitState Orbit
        {
            get => _orbit;
            set
            {
                if (_orbit == value) return;
                _orbit = value;
                UpdateVisuals();
            }
        }

        /// <summary>
        /// color of the rendered trajectory
        /// </summary>
        public UnityEngine.Color Color
        {
            get => _meshRenderer.material.color;
            set => _meshRenderer.material.color = value;
        }

        /// <summary>
        /// stroke width of trajectory line, in px
        /// </summary>
        public int Width
        {
            get => _meshRenderer.material.GetInteger("_Width");
            set => _meshRenderer.material.SetInteger("_Width", value);
        }

        /// <summary>
        /// whether or not to animate the loop progress
        /// </summary>
        public bool DoAnimation
        {
            get => _meshRenderer.material.GetInteger("_DoAnim") != 0;
            set => _meshRenderer.material.SetInteger("_DoAnim", value ? 1 : 0);
        }

        /// <summary>
        /// period of each animation loop
        /// </summary>
        public float AnimationPeriod
        {
            get => _meshRenderer.material.GetFloat("_AnimPeriod");
            set => _meshRenderer.material.SetFloat("_AnimPeriod", value);
        }

        /// <summary>
        /// ( low, high ) alpha across the trajectory
        /// </summary>
        public Vector2 AlphaRange
        {
            get => new(
                _meshRenderer.material.GetFloat("_AlphaLow"),
                _meshRenderer.material.GetFloat("_AlphaHigh")
            );

            set
            {
                _meshRenderer.material.SetFloat("_AlphaLow", value.x);
                _meshRenderer.material.SetFloat("_AlphaHigh", value.y);
            }
        }

        /// <summary>
        /// furthest distance to which we will render parabolic and hyperbolic trajectories (m)
        /// </summary>
        public double maxRenderDistance = 1e13;
        /// <summary>
        /// maximum error allowable in generated trajectory mesh from original conic, as a fraction of semi-latus rectum
        /// NOTE: ideally we want this to maybe be a fraction of the camera size but that would require regenerating the mesh more times than necessary
        /// </summary>
        public double quality = 2e-4;

        /// <summary>
        /// boundaries that restrict the range of true anomalies we draw. nu1 is normalized to [ -PI, +PI ].
        /// note that boundaries here occur time-wise - the orbit will reach nu1 first, then nu2
        /// </summary>
        public double nu1 = double.NegativeInfinity, nu2 = double.PositiveInfinity;

        /// <summary>
        /// whether to clip the trajectory to the current location of the orbit
        /// </summary>
        public bool clipToCurrent = false;

        private void Awake()
        {
            _trajectoryMesh = gameObject.AddComponent<TrajectoryMesh>();
            _meshRenderer = GetComponent<MeshRenderer>();
            _follow = GetComponent<FollowWorldTransform>();
        }

        /// <summary>
        /// data about each vectex in the trajectory mesh
        /// </summary>
        public struct Point
        {
            public Vector2 pos;
            public double r;
            public double nu;
            public double u;
            /// <summary>
            /// construct a trajectory point
            /// </summary>
            /// <param name="r">distance from center</param>
            /// <param name="nu">true anomaly</param>
            /// <param name="u">animation parameter</param>
            /// <param name="o">orbit state</param>
            public Point(double r, double nu, double u, OrbitState o)
            {
                this.r = r; this.nu = nu; this.u = u;
                var theta = (float)(nu + o.omega);
                pos = (float)r * new Vector2(Mathf.Cos(theta), Mathf.Sin(theta));

            }
        }

        // saved orbital parameters

        /// <summary>
        /// coefficient for the determination of universal anomaly
        /// </summary>
        private double coeff;

        private double sqrtGM;

        /// <summary>
        /// characteristic time for this orbit's animation (equivalent to one animation period in real time)
        /// </summary>
        private double tcharacteristic;

        /// <summary>
        /// SOI escape calculator for time scaling
        /// </summary>
        private SOIEscapeTransition _esc;

        /// <summary>
        /// initialize some orbital parameters used for driving the animation
        /// </summary>
        private void InitParams()
        {
            _prop = new UniversalPropagator(_orbit);
            //_esc = new SOIEscapeTransition(_orbit); // TODO: use this to make hyperbolic and parabolic trajectories linear time

            if (double.IsFinite(nu1))
            {
                // normalize nu1 to [-PI, PI)
                var phase = 2 * Math.PI * Math.Floor(nu1 / (2 * Math.PI) + 0.5);
                nu1 -= phase;
                nu2 -= phase;
            }
            else if (double.IsFinite(nu2))
            {
                // one-sided interval: normalize nu2 to [-PI, PI)
                nu2 -= 2 * Math.PI * Math.Floor(nu2 / (2 * Math.PI) + 0.5);
            }


            // cache value so we don't have to recompute it
            coeff = _prop.AnomCoeff;

            sqrtGM = Math.Sqrt(Orbit.GM);

            switch (Orbit.Shape)
            {
                case OrbitShape.Ellipse:
                    // squishes one orbital revolution to [0,1]
                    tcharacteristic = Orbit.period;
                    break;
                case OrbitShape.Parabola:
                    // characteristic time for parabolic orbits:
                    // time-of-flight from periapsis to 90 degree true anomaly
                    tcharacteristic = 4 * Math.Sqrt(Orbit.periapsis * Orbit.periapsis * Orbit.periapsis / Orbit.GM) / 3;
                    break;
                case OrbitShape.Hyperbola:
                    // characteristic time for hyperbolic orbits:
                    //     t = b / v_excess
                    // where b is the impact parameter (distance from asymptote to focus) and
                    // v_excess is the hyperbolic excess velocity (speed at infinity).
                    //
                    // since the formulas for b and v_excess are
                    //     b = -a sqrt( e^2 - 1 )
                    //     vexcess = sqrt( GM / -a )
                    // the expression for the characteristic time is
                    //     t = -a sqrt( -a (e^2 - 1) / GM )
                    tcharacteristic = -Orbit.a * Math.Sqrt(-Orbit.a * (Orbit.e * Orbit.e - 1) / Orbit.GM);
                    break;
            }
        }

        /// <summary>
        /// determine true anomaly bounds for trajectory drawing
        /// </summary>
        private (double, double) CalcNuBounds()
        {
            var dir = Math.Sign(Orbit.h);

            double nuMin, nuMax;
            if (Orbit.Shape == OrbitShape.Ellipse)
            {
                nuMin = double.NegativeInfinity;
                nuMax = double.PositiveInfinity;
            }
            else
            {
                // true anomaly to maxRenderDistance
                double asymptote = Math.Acos((Math.Abs(Orbit.p) - maxRenderDistance) / (maxRenderDistance * Orbit.e));
                nuMin = -asymptote;
                nuMax = asymptote;
            }
            // ensure the two are in the right order chronologically
            nuMin *= dir; nuMax *= dir;

            // proper setters for chonological ordering
            void SetNuMin(double nu) => nuMin = dir == 1 ? Math.Max(nuMin, nu) : Math.Min(nuMin, nu);
            void SetNuMax(double nu) => nuMax = dir == 1 ? Math.Min(nuMax, nu) : Math.Max(nuMax, nu);

            if (clipToCurrent)
            {
                // set lower time bound to orbit position
                var pos = _prop.GetPosition(Universe.Instance.UT);
                var nuPos = Orbit.CalcNu(pos);
                SetNuMin(nuPos);
            }

            // clip orbit

            if (double.IsFinite(nuMin))
            {
                // normalize nuMin to [-PI, PI)
                var phase = 2 * Math.PI * Math.Floor(nuMin / (2 * Math.PI) + 0.5);
                nuMin -= phase;
                nuMax -= phase;
            }
            else if (double.IsFinite(nuMax))
            {
                // one-sided interval: normalize nuMax to [-PI, PI)
                nuMax -= 2 * Math.PI * Math.Floor(nuMax / (2 * Math.PI) + 0.5);
            }

            // if the upper time bound reaches past apoapsis, normalize so we can properly clip with Min and Max
            if (dir * nuMax - 2 * Math.PI > dir * nu1) { nuMin -= dir * 2 * Math.PI; nuMax -= dir * 2 * Math.PI; }
            if (dir * nu2 - 2 * Math.PI > dir * nuMin) { nu1 -= dir * 2 * Math.PI; nu2 -= dir * 2 * Math.PI; }

            // apply clipping
            if (double.IsFinite(nu1)) SetNuMin(nu1);
            if (double.IsFinite(nu2)) SetNuMax(nu2);

            // check if looped
            _trajectoryMesh.isLooped = Math.Abs(nuMax - nuMin) >= 2 * Math.PI;

            // if we are drawing a looped mesh: shift the seam to periapsis so that vertex UV interpolation is sharp
            if (_trajectoryMesh.isLooped) { nuMin = 0; nuMax = 2 * Math.PI; }

            // swap to correct order
            if (nuMin > nuMax) (nuMin, nuMax) = (nuMax, nuMin);

            return (nuMin, nuMax);
        }

        /// <summary>
        /// determine animation parameter at a given true anomaly.
        /// </summary>
        private double CalcU(double nu)
        {
            var danomaly = Orbit.CalcAnomaly(nu) - Orbit.Anomaly0;
            var chi = coeff * danomaly;

            // time since epoch
            var u = _prop.UniversalKepler(chi) / sqrtGM;

            // orbit time for one full cycle of the trajectory animation
            u /= tcharacteristic;

            // shape-specific processing
            switch (Orbit.Shape)
            {
                case OrbitShape.Ellipse:
                    // ensure correct endpoint value at periapsis when the orbit is reversed
                    if (nu == 0.0 && Orbit.h < 0.0) u += 1;
                    break;
                case OrbitShape.Parabola:
                    u = 1 / (1 + Math.Exp(-0.2 * u)); // sigmoid
                    break;
                case OrbitShape.Hyperbola:
                    u = 1 / (1 + Math.Exp(-0.5 * u)); // sigmoid
                    break;
            }

            return u;
        }

        private Point ConstructPoint(double r, double nu) => new Point(r, nu, CalcU(nu), Orbit);

        private void GenerateTrajectory()
        {
            var (nuMin, nuMax) = CalcNuBounds();

            LinkedList<Point> points = new();
            points.AddLast(ConstructPoint(Orbit.GetDistanceFromNu(nuMin), nuMin));
            points.AddLast(ConstructPoint(Orbit.GetDistanceFromNu(nuMax), nuMax));

            // error tolerance
            double tol = Orbit.p * quality;

            // dynamic mesh resolution to match sharpness of orbit's curvature
            void SubdivideMesh(LinkedListNode<Point> start, LinkedListNode<Point> end, double nuStart, double nuEnd, int depth)
            {
                if (depth == 0) return;

                var nuMid = 0.5 * (nuStart + nuEnd);
                // real distance at this point
                var rMid = Orbit.GetDistanceFromNu(nuMid);
                // chord distance based on start and end points (i.e. straight line connecting start to end)
                double r1 = start.Value.r, r2 = end.Value.r;
                var chordMid = 2 * r1 * r2 * Math.Cos(0.5 * (nuEnd - nuStart)) / (r1 + r2);

                // deviates too far
                if (Math.Abs(rMid - chordMid) > tol)
                {
                    var mid = points.AddAfter(start, ConstructPoint(rMid, nuMid));
                    SubdivideMesh(start, mid, nuStart, nuMid, depth - 1);
                    SubdivideMesh(mid, end, nuMid, nuEnd, depth - 1);
                }
            }

            SubdivideMesh(points.First, points.Last, nuMin, nuMax, 64);

            if (_trajectoryMesh.isLooped)
            {
                // remove duplicate point that closes the loop
                points.RemoveLast();

                // reverse the list if necessary so that the first vertex is always u=0
                if (Orbit.h < 0.0)
                {
                    var curr = points.First;
                    while (curr.Next != null)
                    {
                        var next = curr.Next;
                        points.Remove(next);
                        points.AddFirst(next.Value);
                    }
                }
            }

            _trajectoryMesh.SetPointList(points);
            _trajectoryMesh.UpdateMesh();
        }

        public void UpdateVisuals()
        {
            _follow.follow = Orbit.body.transform;

            InitParams();

            GenerateTrajectory();
        }

        private void Update()
        {
            // clipToCurrent requires real-time trajectory updates
            if (clipToCurrent) GenerateTrajectory();
            // maybe try interpolation of cutoff point
        }
    }
}