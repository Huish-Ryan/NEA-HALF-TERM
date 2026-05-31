#nullable enable
using NEA.Models;
using NEA.Rendering;
using NEA.Simulation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace NEA
{
    public partial class MainWindow : Window
    {
        private NBodySimulation? sim = null;
        private Body[] initialBodies = Array.Empty<Body>();
        private float initialDt = 0f;
        private float initialGravity = 0f;

        private readonly VisualCollection visualsHost;
        private readonly DrawingVisual trailsVisual = new();
        private readonly DrawingVisual bodiesVisual = new();

        private Vector3[][] trailBuffers = Array.Empty<Vector3[]>();
        private int[] trailCounts = Array.Empty<int>();
        private int[] trailHeads = Array.Empty<int>();
        private const int TrailLength = 900;

        private CancellationTokenSource? physicsCts = null;
        private Task? physicsTask = null;
        private readonly object simLock = new();

        private float physicsDt = 0.016f;
        private int physicsHz = 100;
        private bool needsRedraw = false;
        private DateTime lastRender = DateTime.MinValue;

        private readonly Stopwatch simulationTimer = new();
        private readonly List<PhysicalRow> physicalRows = new();
        private readonly List<CoordRow> coordRows = new();

        private readonly NEA.Rendering.Camera camera = new();
        private readonly Random rng = new();

        private bool hasScaled = false;

        public MainWindow()
        {
            InitializeComponent();

            visualsHost = RenderSurface.Children;
            visualsHost.Add(trailsVisual);
            visualsHost.Add(bodiesVisual);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadPreset(PresetLibrary.FigureEight());

            InitBodySelector();
            InitGrids();
            SetupTooltips();
            chkCollisions.IsChecked = false;

            simulationTimer.Start();
            CompositionTarget.Rendering += CompositionTarget_Rendering;
            StartPhysicsLoop();

            Dispatcher.InvokeAsync(() =>
            {
                RenderSurface.UpdateLayout();
                AutoScaleCamera();
            }, DispatcherPriority.Render);
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            StopPhysicsLoop();
            CompositionTarget.Rendering -= CompositionTarget_Rendering;
        }

        private void SetupTooltips()
        {
            sldGravity.ToolTip = "Overall gravitational strength (affects all bodies)";
            chkCollisions.ToolTip = "Enable simple velocity swap on collision";
        }

        // ============================================================
        // PHYSICS LOOP
        // ============================================================

        private void StartPhysicsLoop()
        {
            StopPhysicsLoop();
            physicsCts = new CancellationTokenSource();

            physicsTask = Task.Run(async () =>
            {
                var sw = Stopwatch.StartNew();
                double nextTick = sw.Elapsed.TotalSeconds;

                while (!physicsCts.Token.IsCancellationRequested)
                {
                    nextTick += 1.0 / physicsHz;
                    StepPhysics(physicsDt);
                    needsRedraw = true;

                    double wait = nextTick - sw.Elapsed.TotalSeconds;
                    if (wait > 0)
                        await Task.Delay((int)(wait * 1000), physicsCts.Token);
                    else
                        await Task.Yield();
                }
            }, physicsCts.Token);
        }

        private void StopPhysicsLoop()
        {
            physicsCts?.Cancel();
            try { physicsTask?.Wait(200); } catch { }
            physicsCts?.Dispose();
            physicsCts = null;
        }

        private void StepPhysics(float dt)
        {
            lock (simLock)
            {
                if (sim == null) return;

                sim.dt = dt;
                sim.Step();

                for (int i = 0; i < sim.Bodies.Length; i++)
                {
                    int head = trailHeads[i];
                    trailBuffers[i][head] = sim.Bodies[i].position;
                    trailHeads[i] = (head + 1) % TrailLength;
                    if (trailCounts[i] < TrailLength) trailCounts[i]++;
                }
            }
        }

        // ============================================================
        // RENDERING
        // ============================================================

        private void CompositionTarget_Rendering(object? sender, EventArgs e)
        {
            if (!needsRedraw || sim == null) return;

            var now = DateTime.UtcNow;
            if ((now - lastRender).TotalMilliseconds < 16) return;

            lastRender = now;
            needsRedraw = false;

            if (!hasScaled)
                AutoScaleCamera();

            double cx = RenderSurface.ActualWidth / 2;
            double cy = RenderSurface.ActualHeight / 2;

            lock (simLock)
            {
                RenderTrails(cx, cy);
                RenderBodies(cx, cy);
            }

            txtTimer.Text = simulationTimer.Elapsed.ToString(@"mm\:ss");
            UpdateGrids();
        }

        private void RenderTrails(double cx, double cy)
        {
            using var dc = trailsVisual.RenderOpen();
            if (chkOrbitLines.IsChecked != true) return;

            for (int i = 0; i < trailBuffers.Length; i++)
            {
                if (trailCounts[i] < 5) continue;

                var color = (Color)ColorConverter.ConvertFromString(sim!.Bodies[i].hexColour);
                var pen = new Pen(new SolidColorBrush(color), 1.4);

                var geom = new StreamGeometry();
                using (var ctx = geom.Open())
                {
                    int head = trailHeads[i];
                    int count = trailCounts[i];
                    int start = (head - count + TrailLength) % TrailLength;
                    bool first = true;

                    for (int k = 0; k < count; k++)
                    {
                        var p = trailBuffers[i][(start + k) % TrailLength];
                        (double x, double y) = camera.WorldToScreen(p, cx, cy);


                        if (first)
                        {
                            ctx.BeginFigure(new Point(x, y), false, false);
                            first = false;
                        }
                        else
                        {
                            ctx.LineTo(new Point(x, y), true, false);
                        }
                    }
                }

                geom.Freeze();
                dc.DrawGeometry(null, pen, geom);
            }
        }

        private void RenderBodies(double cx, double cy)
        {
            using var dc = bodiesVisual.RenderOpen();

            for (int i = 0; i < sim!.Bodies.Length; i++)
            {
                var b = sim.Bodies[i];
                (double x,double y) = camera.WorldToScreen(b.position, cx, cy);

                double radius = Math.Max(4, Math.Log10(b.mass + 2) * 2.2);

                var brush = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(b.hexColour)
                );

                dc.DrawEllipse(brush, null, new Point(x, y), radius, radius);
            }
        }

        // ============================================================
        // PRESETS + INITIALISATION
        // ============================================================

        private void LoadPreset(Preset preset)
        {
            initialBodies = new Body[preset.Bodies.Length];
            Array.Copy(preset.Bodies, initialBodies, preset.Bodies.Length);

            initialDt = preset.Dt;
            initialGravity = preset.Gravity;

            SetupSimulationFromInitial();
            simulationTimer.Restart();
        }
        private void btnNewInstance_Click(object sender, RoutedEventArgs e)
        {
            var presets = new[]
            {
                PresetLibrary.FigureEight(),
                PresetLibrary.ChaoticTriple(),
                PresetLibrary.NearCollision(),
                PresetLibrary.StableOrbit(),
                PresetLibrary.ThreeEqualMasses()
            };

            var randomPreset = presets[rng.Next(presets.Length)];
            LoadPreset(randomPreset);

            trailCounts = new int[initialBodies.Length];
            trailHeads = new int[initialBodies.Length];
            simulationTimer.Restart();
        }

        private void SetupSimulationFromInitial()
        {
            var copy = new Body[initialBodies.Length];
            Array.Copy(initialBodies, copy, initialBodies.Length);

            lock (simLock)
            {
                sim = new NBodySimulation(copy, initialDt);
                sim.Gravity = initialGravity;
                sim.CollisionsEnabled = chkCollisions.IsChecked == true;
            }

            int n = copy.Length;
            trailBuffers = new Vector3[n][];
            trailCounts = new int[n];
            trailHeads = new int[n];

            for (int i = 0; i < n; i++)
                trailBuffers[i] = new Vector3[TrailLength];

            hasScaled = false;
        }

        private void AutoScaleCamera()
        {
            if (sim == null || sim.Bodies.Length == 0 || RenderSurface.ActualWidth < 50)
                return;

            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;

            foreach (var b in sim.Bodies)
            {
                if (b.position.X < minX) minX = b.position.X;
                if (b.position.X > maxX) maxX = b.position.X;
                if (b.position.Y < minY) minY = b.position.Y;
                if (b.position.Y > maxY) maxY = b.position.Y;
            }

            float width = maxX - minX;
            float height = maxY - minY;

            if (width <= 0 || height <= 0)
                return;

            float padding = 0.15f;
            float zoomX = (float)(RenderSurface.ActualWidth * (1 - padding)) / width;
            float zoomY = (float)(RenderSurface.ActualHeight * (1 - padding)) / height;

            camera.Zoom = Math.Min(zoomX, zoomY);

            float centerX = (minX + maxX) / 2f;
            float centerY = (minY + maxY) / 2f;

            camera.OffsetX = -centerX * camera.Zoom;
            camera.OffsetY = -centerY * camera.Zoom;

            hasScaled = true;
        }

        // ============================================================
        // UI HANDLERS
        // ============================================================

        private void btnStart_Click(object sender, RoutedEventArgs e) => StartPhysicsLoop();

        private void btnPause_Click(object sender, RoutedEventArgs e)
        {
            if (physicsCts != null)
                StopPhysicsLoop();
            else
                StartPhysicsLoop();

            btnPause.Content = physicsCts != null ? "Pause" : "Resume";
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            SetupSimulationFromInitial();
            trailCounts = new int[initialBodies.Length];
            trailHeads = new int[initialBodies.Length];
            simulationTimer.Restart();
            UpdateGrids();
        }

        private void btnFullScreen_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            btnFullScreen.Content = WindowState == WindowState.Maximized ? "Exit Full Screen" : "Full Screen";
        }

        private void sldGravity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sim == null) return;

            sim.Gravity = (float)e.NewValue * 12f;
            txtGravityValue.Text = e.NewValue.ToString("F2");
        }

        private void sldSpeed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            physicsHz = (int)(60 * e.NewValue);
        }

        private void sldMass_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sim == null || cmbBodySelector.SelectedIndex < 0) return;
            int i = cmbBodySelector.SelectedIndex;
            lock (simLock)
                sim.Bodies[i].mass = (float)e.NewValue;
        }

        private void txtHexColour_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sim == null || cmbBodySelector.SelectedIndex < 0) return;
            int i = cmbBodySelector.SelectedIndex;
            lock (simLock)
                sim.Bodies[i].hexColour = txtHexColour.Text;
        }

        private void chkCollisions_Changed(object sender, RoutedEventArgs e)
        {
            if (sim == null) return;
            lock (simLock)
                sim.CollisionsEnabled = chkCollisions.IsChecked == true;
        }

        private void cmbBodySelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sim == null || cmbBodySelector.SelectedIndex < 0) return;
            int i = cmbBodySelector.SelectedIndex;
            var b = sim.Bodies[i];

            sldMass.Value = b.mass;
            txtHexColour.Text = b.hexColour;
        }

        private void btnKepler_Click(object sender, RoutedEventArgs e)
            => ReplaceCurrentBody(1.85f, "#44FFAA");

        private void btnProxima_Click(object sender, RoutedEventArgs e)
            => ReplaceCurrentBody(1.27f, "#88CCFF");

        private void btnDwarf_Click(object sender, RoutedEventArgs e)
            => ReplaceCurrentBody(0.82f, "#FFAA77");

        private void ReplaceCurrentBody(float newMass, string colour)
        {
            if (sim == null || cmbBodySelector.SelectedIndex < 0) return;
            int i = cmbBodySelector.SelectedIndex;

            lock (simLock)
            {
                sim.Bodies[i].mass = newMass;
                sim.Bodies[i].hexColour = colour;
            }

            UpdateGrids();
        }

        private void cmbViewMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            camera.Is3D = cmbViewMode.SelectedIndex == 1;
        }

        private void InitBodySelector()
        {
            cmbBodySelector.Items.Clear();

            for (int i = 0; i < initialBodies.Length; i++)
                cmbBodySelector.Items.Add($"Body {i + 1}");

            cmbBodySelector.SelectedIndex = 0;
        }

        private void InitGrids()
        {
            physicalRows.Clear();
            coordRows.Clear();

            if (sim == null) return;

            for (int i = 0; i < sim.Bodies.Length; i++)
            {
                physicalRows.Add(new PhysicalRow { BodyName = $"Body {i + 1}" });
                coordRows.Add(new CoordRow { BodyName = $"Body {i + 1}" });
            }

            dgPhysicalParams.ItemsSource = physicalRows;
            dgCoords.ItemsSource = coordRows;
        }

        private void UpdateGrids()
        {
            if (sim == null) return;

            lock (simLock)
            {
                for (int i = 0; i < sim.Bodies.Length; i++)
                {
                    var b = sim.Bodies[i];

                    physicalRows[i].Mass = b.mass;
                    physicalRows[i].VelX = b.velocity.X;
                    physicalRows[i].VelY = b.velocity.Y;
                    physicalRows[i].VelZ = b.velocity.Z;

                    coordRows[i].X = b.position.X;
                    coordRows[i].Y = b.position.Y;

                    coordRows[i].D1 = sim.Bodies.Length > 1
                        ? Vector3.Distance(b.position, sim.Bodies[0].position)
                        : 0;

                    coordRows[i].D2 = sim.Bodies.Length > 2
                        ? Vector3.Distance(b.position, sim.Bodies[1].position)
                        : 0;

                    coordRows[i].VelMagnitude = (float)Math.Round(b.velocity.Length(), 2);
                    coordRows[i].Direction =
                        b.velocity.Length() > 0.1f
                        ? $"({Math.Round(b.velocity.X, 1)}, {Math.Round(b.velocity.Y, 1)})"
                        : "—";
                }
            }

            dgPhysicalParams.Items.Refresh();
            dgCoords.Items.Refresh();
        }
    }
}
