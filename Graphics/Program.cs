using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Windows;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

namespace Graphics
{
    public static class Program
    {
        static void Main()
        {
            foreach (var arrow in Arrows())
            {
                var count = 0;
                foreach (Axis axis in Enum.GetValues(typeof(Axis)))
                {
                    if (arrow[axis] != Sign.Zero)
                    {
                        count++;
                    }
                }

                Console.WriteLine(arrow + " -> " + count);
            }

            var form = new RenderForm("SharpDX - MiniCube Direct3D11 Sample");

            // SwapChain description
            var desc = new SwapChainDescription()
            {
                BufferCount = 1,
                ModeDescription =
                    new ModeDescription(form.ClientSize.Width, form.ClientSize.Height,
                                        new Rational(60, 1), Format.R8G8B8A8_UNorm),
                IsWindowed = true,
                OutputHandle = form.Handle,
                SampleDescription = new SampleDescription(1, 0),
                SwapEffect = SwapEffect.Discard,
                Usage = Usage.RenderTargetOutput
            };

            // Used for debugging dispose object references
            // Configuration.EnableObjectTracking = true;

            // Disable throws on shader compilation errors
            //Configuration.ThrowOnShaderCompileError = false;

            // Create Device and SwapChain
            Device device;
            SwapChain swapChain;
            Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.None, desc, out device, out swapChain);
            var context = device.ImmediateContext;

            // Ignore all windows events
            var factory = swapChain.GetParent<Factory>();
            factory.MakeWindowAssociation(form.Handle, WindowAssociationFlags.IgnoreAll);

            // Compile Vertex and Pixel shaders
            var vertexShaderByteCode = ShaderBytecode.CompileFromFile("MiniCube.fx", "VS", "vs_4_0");
            var vertexShader = new VertexShader(device, vertexShaderByteCode);

            var pixelShaderByteCode = ShaderBytecode.CompileFromFile("MiniCube.fx", "PS", "ps_4_0");
            var pixelShader = new PixelShader(device, pixelShaderByteCode);

            var signature = ShaderSignature.GetInputSignature(vertexShaderByteCode);
            // Layout from VertexShader input signature
            var layout = new InputLayout(device, signature, new[]
                    {
                        new InputElement("POSITION", 0, Format.R32G32B32A32_Float, 0, 0),
                        new InputElement("COLOR", 0, Format.R32G32B32A32_Float, 16, 0)
                    });

            var rubiksCube = RubikAlgebra.InitialCube;

            // Instantiate Vertex buiffer from vertex data
            var vertexIns = rubiksCube.EnumerateRubix().ToArray();

            var vertices = Buffer.Create(device, BindFlags.VertexBuffer, vertexIns);

            // Create Constant Buffer
            var contantBuffer = new Buffer(device, Utilities.SizeOf<Matrix>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            
            // Prepare All the stages
            context.InputAssembler.InputLayout = layout;
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertices, Utilities.SizeOf<Vector4>() * 2, 0));
            context.VertexShader.SetConstantBuffer(0, contantBuffer);
            context.VertexShader.Set(vertexShader);
            context.PixelShader.Set(pixelShader);

            // Prepare matrices
            var view = Matrix.LookAtLH(new Vector3(0, 0, -5), new Vector3(0, 0, 0), Vector3.UnitY);
            Matrix proj = Matrix.Identity;

            // Use clock
            var clock = new Stopwatch();
            clock.Start();

            // Declare texture for rendering
            bool userResized = true;
            Texture2D backBuffer = null;
            RenderTargetView renderView = null;
            Texture2D depthBuffer = null;
            DepthStencilView depthView = null;
            Arrow? userTurn = null;
            Arrow userRotate = new Arrow();
            Matrix userRotation = Matrix.Identity;
            
            // Setup handler on resize form
            form.UserResized += (sender, args) => userResized = true;

            // Setup full screen mode change F5 (Full) F4 (Window)
            form.KeyDown += (sender, args) =>
            {
                var rotate = args.KeyCode.DetectUserRotation();

                if (userRotate.Dot(rotate) <= 0)
                {
                    userRotate = userRotate.Add(rotate);
                }

                userTurn = userTurn ?? args.KeyCode.DetectUserTurn();
            };

                // Setup full screen mode change F5 (Full) F4 (Window)
            form.KeyUp += (sender, args) =>
            {
                var rotate = args.KeyCode.DetectUserRotation().Negate();

                if (userRotate.Dot(rotate) <= 0)
                {
                    userRotate = userRotate.Add(rotate);
                }

                if (args.KeyCode == Keys.Enter)
                    userRotation = Matrix.Identity;
                else if (args.KeyCode == Keys.F5)
                    swapChain.SetFullscreenState(true, null);
                else if (args.KeyCode == Keys.F4)
                    swapChain.SetFullscreenState(false, null);
                else if (args.KeyCode == Keys.Escape)
                    form.Close();
            };

            // Main loop
            RenderLoop.Run(form, () =>
            {
                // If Form resized
                if (userResized)
                {
                    // Dispose all previous allocated resources
                    Utilities.Dispose(ref backBuffer);
                    Utilities.Dispose(ref renderView);
                    Utilities.Dispose(ref depthBuffer);
                    Utilities.Dispose(ref depthView);

                    // Resize the backbuffer
                    swapChain.ResizeBuffers(desc.BufferCount, form.ClientSize.Width, form.ClientSize.Height, Format.Unknown, SwapChainFlags.None);

                    // Get the backbuffer from the swapchain
                    backBuffer = Texture2D.FromSwapChain<Texture2D>(swapChain, 0);

                    // Renderview on the backbuffer
                    renderView = new RenderTargetView(device, backBuffer);

                    // Create the depth buffer
                    depthBuffer = new Texture2D(device, new Texture2DDescription()
                    {
                        Format = Format.D32_Float_S8X24_UInt,
                        ArraySize = 1,
                        MipLevels = 1,
                        Width = form.ClientSize.Width,
                        Height = form.ClientSize.Height,
                        SampleDescription = new SampleDescription(1, 0),
                        Usage = ResourceUsage.Default,
                        BindFlags = BindFlags.DepthStencil,
                        CpuAccessFlags = CpuAccessFlags.None,
                        OptionFlags = ResourceOptionFlags.None
                    });

                    // Create the depth buffer view
                    depthView = new DepthStencilView(device, depthBuffer);

                    // Setup targets and viewport for rendering
                    context.Rasterizer.SetViewport(new Viewport(0, 0, form.ClientSize.Width, form.ClientSize.Height, 0.0f, 1.0f));
                    context.OutputMerger.SetTargets(depthView, renderView);

                    // Setup new projection matrix with correct aspect ratio
                    proj = Matrix.PerspectiveFovLH((float)Math.PI / 4.0f, form.ClientSize.Width / (float)form.ClientSize.Height, 0.1f, 100.0f);

                    // We are done resizing
                    userResized = false;
                }

                if (userTurn != null)
                {
                    rubiksCube = new FaceTurn(userTurn.Value).TurnCube().Morph(rubiksCube);

                    vertexIns = rubiksCube.EnumerateRubix().ToArray();

                    context.UpdateSubresource(vertexIns, vertices);

                    userTurn = null;
                }

                var time = 1.0f / 60.0f / 25.0f;

                var viewProj = Matrix.Multiply(view, proj);
                
                // Clear views
                context.ClearDepthStencilView(depthView, DepthStencilClearFlags.Depth, 1.0f, 0);
                context.ClearRenderTargetView(renderView, Color.Gray);

                userRotation = userRotation * Matrix.RotationX(-(int)userRotate.Y * time) * Matrix.RotationY((int)userRotate.X * time) * Matrix.RotationZ((int)userRotate.Z * time);

                // Update WorldViewProj Matrix
                var worldViewProj = userRotation * viewProj;
                worldViewProj.Transpose();
                context.UpdateSubresource(ref worldViewProj, contantBuffer);

                // Draw the cube
                context.Draw(vertexIns.Length / 2, 0);

                // Present!
                swapChain.Present(0, PresentFlags.None);
            });

            // Release all resources
            signature.Dispose();
            vertexShaderByteCode.Dispose();
            vertexShader.Dispose();
            pixelShaderByteCode.Dispose();
            pixelShader.Dispose();
            vertices.Dispose();
            layout.Dispose();
            contantBuffer.Dispose();
            depthBuffer.Dispose();
            depthView.Dispose();
            renderView.Dispose();
            backBuffer.Dispose();
            context.ClearState();
            context.Flush();
            device.Dispose();
            context.Dispose();
            swapChain.Dispose();
            factory.Dispose();
        }

        private static Arrow DetectUserRotation(this Keys key)
        {
                switch (key)
            {
                case Keys.Up:
                    return Axis.Y.AsArrow();
                case Keys.Down:
                    return Axis.Y.AsArrow().Negate();
                case Keys.Left:
                    return Axis.X.AsArrow().Negate();
                case Keys.Right:
                    return Axis.X.AsArrow();
                default:
                    return new Arrow(); ;
            }
        }

        private static Arrow? DetectUserTurn(this Keys key)
        {
            switch (key)
            {
                case Keys.U:
                    return Axis.Y.AsArrow();
                case Keys.D:
                    return Axis.Y.AsArrow().Negate();
                case Keys.F:
                    return Axis.Z.AsArrow().Negate();
                case Keys.B:
                    return Axis.Z.AsArrow();
                case Keys.L:
                    return Axis.X.AsArrow().Negate();
                case Keys.R:
                    return Axis.X.AsArrow();
                default:
                    return null;
            }
        }

        private static IEnumerable<Vector4> EnumerateRubix(this IRubik cube)
        {
            foreach (var arrow in Arrows())
            {
                var offset = new Vector4((int)arrow.X * 3.0f, (int)arrow.Y * 3.0f, (int)arrow.Z * 3.0f, 0.0f);

                var cubieVertices = EnumerateCube(3.0f, offset,
                    color: face =>
                    {
                        if (face.Dot(arrow) > 0)
                        {
                            var color = cube[new CubieFace(arrow, face)];

                            return color.AsColor();
                        }

                        return new Vector4();
                    });

                foreach (var vertexIn in cubieVertices)
                {
                    yield return vertexIn;
                }
            }
        }

        private static Vector4 AsVector(this Arrow arrow, float scale)
        {
            return new Vector4((int) arrow.X * 1.0f, (int) arrow.Y * 1.0f, (int) arrow.Z * 1.0f, scale);
        }

        private static Vector4 AsColor(this Colors color)
        {
            switch (color)
            {
                case Colors.Green:

                    return new Vector4(0.0f, 1.0f, 0.0f, 1.0f);
                case Colors.Blue:

                    return new Vector4(0.0f, 0.0f, 1.0f, 1.0f);
                case Colors.Red:

                    return new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
                case Colors.Orange:

                    return new Vector4(1.0f, 0.5f, 0.0f, 1.0f);
                case Colors.White:

                    return new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
                case Colors.Yellow:

                    return new Vector4(1.0f, 1.0f, 0.0f, 1.0f);
                default:
                    throw new ArgumentOutOfRangeException(nameof(color), color, null);
            }
        }

        private static IEnumerable<Vector4> SquareVertices(this Arrow direction, float scale, Vector4 position)
        {
            var corner = new Arrow(Sign.Negative, Sign.Negative, Sign.Negative);

            var projection = direction.Scale((Sign)direction.Dot(corner));

            var vertex = corner.Add(projection.Negate());

            yield return vertex.AsVector(scale) + position;

            vertex = direction.Cross(vertex);

            yield return vertex.AsVector(scale) + position;

            vertex = direction.Cross(vertex);

            yield return vertex.AsVector(scale) + position;

            vertex = direction.Cross(vertex);

            yield return vertex.AsVector(scale) + position;
        }

        private static IEnumerable<Vector4> EnumerateCube(float scale, Vector4 position, Func<Arrow, Vector4> color)
        {
            foreach (var face in Directions())
            {
                foreach (var vertexIn in EnumeratePolygon(color(face), face.CubeFaceVertices(scale, position)))
                {
                    yield return vertexIn;
                }
            }
        }

        private static Vector4[] CubeFaceVertices(this Arrow face, float scale, Vector4 position)
        {
            return face.SquareVertices(scale, face.AsVector(0.0f) + position).ToArray();
        }

        public static IEnumerable<Vector4> EnumeratePolygon(Vector4 color, params Vector4[] vertices)
        {
            if (vertices.Length < 3)
            {
                throw new ArgumentException("Polygon must have at least 3 vertices.");
            }

            for (int index = 0; index + 2 < vertices.Length; index++)
            {
                yield return vertices[0];
                yield return color;
                yield return vertices[index+1];
                yield return color;
                yield return vertices[index+2];
                yield return color;
            }
        }

        public static void Main2(string[] args)
        {
            //foreach (var matrix in Matrices())
            //{
            //    Console.WriteLine(string.Format("{0}", matrix.Determinant()));
            //}

            //Console.ReadLine();

            foreach (var cubie in Arrows())
                foreach (var face in Basis().Where(_ => cubie.Dot(_) == 1))
                    foreach (var direction in Basis().Where(_ => face.Dot(_) == 0))
                    {
                        CubieFace result = new CubieFace(cubie, face);

                        Console.WriteLine(string.Format("{0} -> {1} = {2}", result, direction, result.Loop(direction)));
                    }

            Console.ReadLine();

            foreach (var first in Arrows())
                foreach (var second in Basis())
                {
                    var cross = second.Rotate().Morph(first);

                    Console.WriteLine(string.Format("{0} x {1} = {2}", first, second, cross));

                    if (first.Magnitude() * second.Magnitude() == cross.Magnitude())
                    {
                        Console.WriteLine(string.Format("{0} * {1} = {2} Ok", first.Magnitude(), second.Magnitude(), cross.Magnitude()));
                    }
                    else
                    {
                        Console.WriteLine(string.Format("{0} * {1} = {2}", first.Magnitude(), second.Magnitude(), cross.Magnitude()));
                    }
                }

            Console.ReadLine();

            foreach (var face in Basis())
                foreach (var cubie in Arrows())
                {
                    var rotated = face.Rotate().Power(4).Morph(cubie);

                    Console.WriteLine(string.Format("{0} ~> {1} = {2}", cubie, face, rotated));
                }

            Console.ReadLine();

            uint count = 0;
            foreach (var cubie in Arrows())
                foreach (var face in Basis().Where(_ => cubie.Dot(_) == 1))
                {
                    Console.WriteLine(string.Format("{0} : {1}", cubie, face));
                    count++;
                }

            Console.WriteLine("Faces: " + count);

            Console.ReadLine();

            foreach (var cubie in Arrows())
                foreach (var face in Basis().Where(_ => cubie.Dot(_) == 1))
                    foreach (var direction in Basis().Where(_ => face.Dot(_) == 0))
                    {
                        CubieFace result = new CubieFace(cubie, face);

                        FollowDirections(result, direction);
                    }

            Console.ReadLine();

            foreach (var arrow in Arrows())
            {
                Console.Write(arrow);

                foreach (var direction in Basis().Where(_ => arrow.Dot(_) == 0))
                {
                    Console.WriteLine(" -> " + arrow);

                }

                Console.WriteLine(arrow);
            }

            Console.ReadLine();
        }

        public static CubieFace FollowDirections(CubieFace result, Arrow direction)
        {
            foreach (var _ in Enumerable.Repeat<object>(null, 12))
            {
                var next = result.Neighbour(direction);

                Console.WriteLine(string.Format("{0} : {1} -> {2} = {3} : {4}", result.Cubie, result.Face, direction, next.Cubie, next.Face));

                direction = result.ReOrient(direction);
                result = next;
            }

            Console.ReadLine();

            return result;
        }

        public static IEnumerable<Arrow> Basis()
        {
            return Arrows().Where(arrow => arrow.Dot(arrow) == 1);
        }

        public static IEnumerable<Arrow> Arrows()
        {
            foreach (Sign x in Enum.GetValues(typeof(Sign)))
                foreach (Sign y in Enum.GetValues(typeof(Sign)))
                    foreach (Sign z in Enum.GetValues(typeof(Sign)))
                    {
                        var arrow = new Arrow(x, y, z);

                        yield return arrow;
                    }
        }

        public static IEnumerable<Arrow> Directions()
        {
            foreach (Axis axis in Enum.GetValues(typeof(Axis)))
            {
                yield return axis.AsArrow();
                yield return axis.AsArrow().Negate();
            }
        }

        public static IEnumerable<Sign[,]> Matrices()
        {
            foreach (Sign a in Enum.GetValues(typeof(Sign)))
                foreach (Sign b in Enum.GetValues(typeof(Sign)))
                    foreach (Sign c in Enum.GetValues(typeof(Sign)))
                        foreach (Sign d in Enum.GetValues(typeof(Sign)))
                        {
                            var matrix = new Sign[2, 2] {
                    { a, b },
                    { c, d },
                };

                            yield return matrix;
                        }
        }

    }
}
