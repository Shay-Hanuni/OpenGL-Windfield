using System;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System.Drawing;
using System.Drawing.Imaging;

namespace OpenGLBase
{
    class Program
    {
        static void Main(string[] args)
        {
            using (Game game = new Game())
            {
                game.Run(60.0);
            }
        }
    }

    class Game : GameWindow
    {
        private double totalTime = 0.0;
        private float ambientLight = 0.2f;
        private float headHeightOffset = 0f;
        private float cameraAngleX = 25f;
        private float cameraAngleY = 0f;
        private float zoom = -50f;
        private Vector2 lastMousePos;
        private bool isDragging = false;

        private float sunAngle = 0f;
        private float moonAngle = 180f;
        private Random rand = new Random();
        private Vector3[] stars = new Vector3[200];

        private int btnLeftTex, btnRightTex;
        private int[] btnWindmillTex = new int[6];
        private int btnStopTex, btnStartTex, btnSwitchTex;

        private bool buttonLeftPressed = false;
        private bool buttonRightPressed = false;

        private float[] windmillRotations = new float[5];
        private int selectedWindmill = 0;
        private bool isPaused = false;
        


        public Game() : base(800, 600, GraphicsMode.Default, "Wind Field Project")
        {
            VSync = VSyncMode.On;
            for (int i = 0; i < stars.Length; i++)
            {
                float x = (float)(rand.NextDouble() * 40 - 20);
                float y = (float)(rand.NextDouble() * 20 + 10);
                float z = (float)(rand.NextDouble() * 40 - 20);
                stars[i] = new Vector3(x, y, z);
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            GL.Enable(EnableCap.Blend); // מוסיף תמיכה בשקיפות
            GL.Enable(EnableCap.DepthTest);
            GL.ClearStencil(0);
            GL.Clear(ClearBufferMask.StencilBufferBit);

            System.Drawing.Font font = new System.Drawing.Font("Arial", 24);
            btnLeftTex = CreateTextTexture("      LEFT", font, System.Drawing.Color.White, System.Drawing.Color.LightGreen);
            btnRightTex = CreateTextTexture("     RIGHT", font, System.Drawing.Color.White, System.Drawing.Color.Pink);
            btnWindmillTex[0] = CreateTextTexture("      ALL", font, Color.White, Color.White);
            for (int i = 1; i <= 5; i++)
            {
                string paddedText = "      WM" + i.ToString() + "";
                btnWindmillTex[i] = CreateTextTexture(paddedText, font, Color.White, Color.White);
            }
            btnStopTex = CreateTextTexture("    STOP", font, Color.White, Color.White);
            btnStartTex = CreateTextTexture("    START", font, Color.White, Color.White);
            btnSwitchTex = CreateTextTexture("   SWITCH", font, Color.White, Color.White);
        } // סוף OnLoad
        private void DrawTexturedButton(int x, int y, int width, int height, int texture)
        {
            GL.Enable(EnableCap.Texture2D);
            GL.BindTexture(TextureTarget.Texture2D, texture);

            GL.Begin(PrimitiveType.Quads);
            GL.Color3(1f, 1f, 1f);

            GL.TexCoord2(0, 1); GL.Vertex2(x, y);
            GL.TexCoord2(1, 1); GL.Vertex2(x + width, y);
            GL.TexCoord2(1, 0); GL.Vertex2(x + width, y + height);
            GL.TexCoord2(0, 0); GL.Vertex2(x, y + height);
            GL.End();

            GL.Disable(EnableCap.Texture2D);
        }


        private int CreateTextTexture(string text, Font font, Color textColor, Color backColor)
        {
            // השתמש בצבע רקע שקוף
            Bitmap bmp = new Bitmap(256, 64, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using (Graphics gfx = Graphics.FromImage(bmp))
            {
                gfx.Clear(Color.Transparent); // ✅ רקע שקוף
                gfx.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                using (Brush textBrush = new SolidBrush(textColor))
                {
                    gfx.DrawString(text, font, textBrush, new PointF(0, 0));
                }
            }

            int textureId = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, textureId);

            BitmapData data = bmp.LockBits(
                new Rectangle(0, 0, bmp.Width, bmp.Height),
                ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                data.Width, data.Height, 0,
                OpenTK.Graphics.OpenGL.PixelFormat.Bgra,
                PixelType.UnsignedByte, data.Scan0);

            bmp.UnlockBits(data);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            return textureId;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, Width, Height);
            Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(45f),
                Width / (float)Height,
                0.1f, 100f);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref projection);
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            var keyboard = Keyboard.GetState();
            if (keyboard.IsKeyDown(Key.Escape)) Exit();

            if (!isPaused)
            {
                totalTime += e.Time;

                float angleSpeed = 360f / 50f;
                sunAngle += angleSpeed * (float)e.Time;

                if (sunAngle <= 180f)
                {
                    float sunHeight = (float)Math.Sin(MathHelper.DegreesToRadians(sunAngle));
                    ambientLight = MathHelper.Clamp(sunHeight, 0.2f, 1.0f);
                    moonAngle = (sunAngle+180f)%360; // לא מוצג
                }
                else if (sunAngle > 180f && sunAngle < 360f)
                {
                    ambientLight = 0.2f; // חושך קבוע
                    moonAngle = sunAngle - 180f;
                }
                else
                {
                    sunAngle = 0f;
                    moonAngle = 180f;
                }

                if (keyboard.IsKeyDown(Key.Left) || buttonLeftPressed)
                {
                    if (selectedWindmill == 0)
                        for (int i = 0; i < 5; i++) windmillRotations[i] -= 60f * (float)e.Time;
                    else
                        windmillRotations[selectedWindmill - 1] -= 60f * (float)e.Time;
                }

                if (keyboard.IsKeyDown(Key.Right) || buttonRightPressed)
                {
                    if (selectedWindmill == 0)
                        for (int i = 0; i < 5; i++) windmillRotations[i] += 60f * (float)e.Time;
                    else
                        windmillRotations[selectedWindmill - 1] += 60f * (float)e.Time;
                }
            }

            // תאורה – מתעדכן תמיד לפי מיקום השמש
            if (sunAngle >= 10f && sunAngle <= 170f)
            {
                float sunHeight = (float)Math.Sin(MathHelper.DegreesToRadians(sunAngle));
                ambientLight = MathHelper.Clamp(sunHeight, 0.2f, 1.0f);
            }
            else
            {
                ambientLight = 0.2f;
            }
        }
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            float background = ambientLight;
            GL.ClearColor(background * 0.4f, background * 0.7f, background * 1.0f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            GL.Translate(0, 0, zoom);
            GL.Rotate(cameraAngleX, 1, 0, 0);
            GL.Rotate(cameraAngleY, 0, 1, 0);

            // שלב 0: ציור הקרקע
             DrawGrassBase();
            RenderReflection();
            // שלב 1: ציור שטח האגם ל־Stencil בלבד
            GL.Enable(EnableCap.StencilTest);
            GL.StencilOp(StencilOp.Replace, StencilOp.Replace, StencilOp.Replace);
            GL.StencilFunc(StencilFunction.Always, 1, 0xFF);

            GL.ColorMask(false, false, false, false);
            GL.DepthMask(false);
            DrawLake(); // רק ל־Stencil
            GL.ColorMask(true, true, true, true);
            GL.DepthMask(true);

            // שלב 2: ציור השתקפות – רק באגם – רק מה שמעל המים
            GL.StencilFunc(StencilFunction.Equal, 1, 0xFF);
            GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Keep);

            GL.Enable(EnableCap.ClipPlane0);
            double[] reflectionClipPlane = { 0.0, 1.0, 0.0, -0.5 }; // חותך את כל מה שמתחת ל־Y=0
            GL.ClipPlane(ClipPlaneName.ClipPlane0, reflectionClipPlane);

            GL.PushMatrix();
            GL.Scale(1f, -1f, 1f); // הפוך על ציר Y

            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);
            for (int i = 0; i < 5; i++)
                DrawWindmill(totalTime, GetWindmillPosition(i), windmillRotations[i]);
            DrawSunAndMoon();

            GL.CullFace(CullFaceMode.Front);
            for (int i = 0; i < 5; i++)
                DrawWindmill(totalTime, GetWindmillPosition(i), windmillRotations[i]);
            GL.Disable(EnableCap.CullFace);

            GL.PopMatrix();

            GL.Disable(EnableCap.ClipPlane0);

            // שלב 3: ציור האגם עצמו – עם שקיפות
            GL.DepthMask(false);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            DrawLake(); // עם GL.Color4(..., alpha)
            GL.Disable(EnableCap.Blend);
            GL.DepthMask(true);

            GL.Disable(EnableCap.StencilTest);

            // שלב 4: דשא מתנדנד
            GL.PushMatrix();
            GL.Translate(0, -0.01f, 0);
            DrawGrassField(totalTime);
            GL.PopMatrix();
            DrawGrassField(totalTime);



            // שלב 5: ציור הצל – רק כשהשמש מעל הקרקע (ביום)
            if (sunAngle <= 170f && sunAngle>=10)
            {
                float sunAngleRad = MathHelper.DegreesToRadians(sunAngle);
                float sunX = (float)Math.Cos(sunAngleRad) * 100f;
                float sunY = (float)Math.Max(0.2f, Math.Sin(sunAngleRad)) * 100f;
                float sunZ = 0f;

                float[] lightPos = { -sunX, -sunY, -sunZ, 0.0f };

               // Console.WriteLine($"[DEBUG] sunAngle: {sunAngle}, lightDir: ({lightPos[0]}, {lightPos[1]}, {lightPos[2]})");

                for (int i = 0; i < 5; i++)
                {
                    Vector3 pos = GetWindmillPosition(i);
                    Vector3[] tri = new Vector3[]
                    {
            new Vector3(-1 + pos.X, 1, -1 + pos.Z),
            new Vector3(1 + pos.X, 1, -1 + pos.Z),
            new Vector3(0 + pos.X, 1, 1 + pos.Z)
                    };

                    float[,] ground = new float[3, 3]
                    {
            { tri[0].X, tri[0].Y, tri[0].Z },
            { tri[1].X, tri[1].Y, tri[1].Z },
            { tri[2].X, tri[2].Y, tri[2].Z }
                    };

                    float[] shadowPlane = new float[4];
                    float[] normal = new float[3];

                    float[] v1 = {
            ground[0,0] - ground[1,0],
            ground[0,1] - ground[1,1],
            ground[0,2] - ground[1,2]
        };
                    float[] v2 = {
            ground[1,0] - ground[2,0],
            ground[1,1] - ground[2,1],
            ground[1,2] - ground[2,2]
        };

                    normal[0] = v1[1] * v2[2] - v1[2] * v2[1];
                    normal[1] = v1[2] * v2[0] - v1[0] * v2[2];
                    normal[2] = v1[0] * v2[1] - v1[1] * v2[0];
                    float len = (float)Math.Sqrt(normal[0] * normal[0] + normal[1] * normal[1] + normal[2] * normal[2]);
                    normal[0] /= len; normal[1] /= len; normal[2] /= len;

                    shadowPlane[0] = normal[0];
                    shadowPlane[1] = normal[1];
                    shadowPlane[2] = normal[2];
                    shadowPlane[3] = -(normal[0] * ground[2, 0] + normal[1] * ground[2, 1] + normal[2] * ground[2, 2]);

                    float dot = shadowPlane[0] * lightPos[0] + shadowPlane[1] * lightPos[1] + shadowPlane[2] * lightPos[2] + shadowPlane[3] * lightPos[3];

                    float[] shadowMatrix = new float[16];
                    shadowMatrix[0] = dot - lightPos[0] * shadowPlane[0];
                    shadowMatrix[4] = -lightPos[0] * shadowPlane[1];
                    shadowMatrix[8] = -lightPos[0] * shadowPlane[2];
                    shadowMatrix[12] = -lightPos[0] * shadowPlane[3];

                    shadowMatrix[1] = -lightPos[1] * shadowPlane[0];
                    shadowMatrix[5] = dot - lightPos[1] * shadowPlane[1];
                    shadowMatrix[9] = -lightPos[1] * shadowPlane[2];
                    shadowMatrix[13] = -lightPos[1] * shadowPlane[3];

                    shadowMatrix[2] = -lightPos[2] * shadowPlane[0];
                    shadowMatrix[6] = -lightPos[2] * shadowPlane[1];
                    shadowMatrix[10] = dot - lightPos[2] * shadowPlane[2];
                    shadowMatrix[14] = -lightPos[2] * shadowPlane[3];

                    shadowMatrix[3] = -lightPos[3] * shadowPlane[0];
                    shadowMatrix[7] = -lightPos[3] * shadowPlane[1];
                    shadowMatrix[11] = -lightPos[3] * shadowPlane[2];
                    shadowMatrix[15] = dot - lightPos[3] * shadowPlane[3];

                    GL.Disable(EnableCap.Lighting);
                    GL.Disable(EnableCap.DepthTest);
                    GL.PushMatrix();
                    GL.MultMatrix(shadowMatrix);
                    GL.Translate(pos + new Vector3(0f, 1.01f, 0f));

                    GL.Enable(EnableCap.Blend);
                    GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                    GL.Color4(0.0f, 0.0f, 0.0f, 0.3f); // ✅ אדום אטום — DEBUG

                    DrawWindmillShadowModel(0, windmillRotations[i], totalTime);


                    GL.Disable(EnableCap.Blend);
                    GL.PopMatrix();
                    GL.Enable(EnableCap.DepthTest);
                    GL.Enable(EnableCap.Lighting);
                }
            }



            // שלב 6: ציור תחנות רגילות
            GL.Disable(EnableCap.Lighting);
                for (int i = 0; i < 5; i++)
                    DrawWindmill(totalTime, GetWindmillPosition(i), windmillRotations[i]);
                GL.Enable(EnableCap.Lighting);

                DrawSunAndMoon();
                if (ambientLight == 0.2f && sunAngle >= 180f)
                    DrawStars((int)(totalTime) % 2 == 0);

                DrawButtons();
                SwapBuffers();
            }

        // === ציור השתקפות באגם לפי שיטת המרצה ===

        protected void RenderReflection()
        {
            // שלב 1: סימון צורת האגם בתוך ה־Stencil בלבד
            GL.Enable(EnableCap.StencilTest);
            GL.Clear(ClearBufferMask.StencilBufferBit);
            GL.StencilFunc(StencilFunction.Always, 1, 0xFF);
            GL.StencilOp(StencilOp.Replace, StencilOp.Replace, StencilOp.Replace);
            GL.ColorMask(false, false, false, false);
            GL.DepthMask(false);
            DrawLake(); // ציור רק לצורך יצירת צורת האגם
            GL.ColorMask(true, true, true, true);
            GL.DepthMask(true);

            // שלב 2: ציור השתקפות תחנות הרוח בלבד
            GL.StencilFunc(StencilFunction.Equal, 1, 0xFF);
            GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Keep);

            GL.Disable(EnableCap.Lighting);
            GL.PushMatrix();
            GL.Translate(0.0f, 0.6f, 0.0f);
            GL.Scale(1.0f, -1.0f, 1.0f);
            GL.Translate(0.0f, -0.5f, 0.0f);

            for (int i = 0; i < 5; i++)
                DrawWindmill(totalTime, GetWindmillPosition(i), windmillRotations[i]);

            GL.PopMatrix();
            GL.Enable(EnableCap.Lighting);

            // שלב 2.5: דיסק שמש משוקף
            if (sunAngle < 180f)
            {
                float sunX = (float)Math.Cos(MathHelper.DegreesToRadians(sunAngle)) * 22f;
                float sunY = (float)Math.Sin(MathHelper.DegreesToRadians(sunAngle)) * 15f;
                float sunZ = 0f;
                float dist = (float)Math.Sqrt(sunX * sunX + sunZ * sunZ);

                if (dist < 9f && sunY > 0.5f)
                {
                    GL.Disable(EnableCap.Lighting);
                    GL.PushMatrix();
                    GL.Translate(sunX, 0.01f, sunZ);
                    GL.Rotate(-90, 1, 0, 0);
                    GL.Color3(1f, 1f, 0f);
                    DrawDisk(1.5f, 30);
                    GL.PopMatrix();
                    GL.Enable(EnableCap.Lighting);
                }
            }

            // שלב 2.6: דיסק ירח משוקף
            if (moonAngle < 180f)
            {
                float moonX = (float)Math.Cos(MathHelper.DegreesToRadians(moonAngle)) * 22f;
                float moonY = (float)Math.Sin(MathHelper.DegreesToRadians(moonAngle)) * 15f;
                float moonZ = 0f;
                float dist = (float)Math.Sqrt(moonX * moonX + moonZ * moonZ);

                if (dist < 9f && moonY > 0.5f)
                {
                    GL.Disable(EnableCap.Lighting);
                    GL.PushMatrix();
                    GL.Translate(moonX, 0.01f, moonZ);
                    GL.Rotate(-90, 1, 0, 0);
                    GL.Color3(0.9f, 0.9f, 1f);
                    DrawDisk(1.5f, 30);
                    GL.PopMatrix();
                    GL.Enable(EnableCap.Lighting);
                }
            }

            GL.Disable(EnableCap.StencilTest);

            // שלב 3: ציור האגם עם שקיפות כדי לחשוף את ההשתקפות
            GL.Disable(EnableCap.Lighting);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            DrawLake();
            GL.Disable(EnableCap.Blend);
            GL.Enable(EnableCap.Lighting);
        }

        private void DrawDisk(float radius, int segments)
        {
            GL.Begin(PrimitiveType.TriangleFan);
            GL.Vertex3(0.0f, 0.0f, 0.0f); // מרכז

            for (int i = 0; i <= segments; i++)
            {
                double angle = 2 * Math.PI * i / segments;
                float x = (float)Math.Cos(angle) * radius;
                float y = (float)Math.Sin(angle) * radius;
                GL.Vertex3(x, y, 0.0f);
            }

            GL.End();
        }




        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButton.Left)
            {
                int mx = e.X;
                int my = Height - e.Y;

                int buttonSize = 60;
                int spacing = 10;
                int totalButtons = 6;
                int totalWidth = totalButtons * buttonSize + (totalButtons - 1) * spacing;
                int startX = (Width - totalWidth) / 2;
                int yStation = 50;

                for (int i = 0; i < 6; i++)
                {
                    int bx = startX + i * (buttonSize + spacing);
                    if (mx >= bx && mx <= bx + buttonSize && my >= yStation && my <= yStation + buttonSize)
                    {
                        selectedWindmill = i;
                        return;
                    }
                }

                int centerX = Width / 2;
                int yControl = yStation + buttonSize + 20;
                if (mx >= centerX - 80 && mx <= centerX - 10 && my >= yControl && my <= yControl + 50)
                    buttonLeftPressed = true;
                else if (mx >= centerX + 10 && mx <= centerX + 80 && my >= yControl && my <= yControl + 50)
                    buttonRightPressed = true;

                // כפתורים בצד ימין
                int sideX = Width - 90;
                int sideY = 60;
                int sideButtonSize = 60;

                if (mx >= sideX && mx <= sideX + sideButtonSize)
                {
                    if (my >= sideY && my <= sideY + sideButtonSize)
                    {
                        isPaused = true;
                    }
                    else if (my >= sideY + 70 && my <= sideY + 70 + sideButtonSize)
                    {
                        isPaused = false;
                    }
                    else if (my >= sideY + 140 && my <= sideY + 140 + sideButtonSize)
                    {
                        // SWITCH = החלפת שמש וירח חכמה
                        if (sunAngle <= 180f)
                        {
                            // השמש פעילה – הירח מקבל את זווית השמש, ואז השמש נעלמת
                            moonAngle = sunAngle;
                            sunAngle = (moonAngle + 180f) % 360f;
                        }
                        else
                        {
                            // הירח פעיל – השמש חוזרת לזווית שהייתה לירח
                            sunAngle = moonAngle;
                            moonAngle = (sunAngle + 180f)%360;
                        }



                        // עדכון תאורה לפי המצב החדש
                        if (sunAngle <= 180f)
                        {
                            float sunHeight = (float)Math.Sin(MathHelper.DegreesToRadians(sunAngle));
                            ambientLight = MathHelper.Clamp(sunHeight, 0.2f, 1.0f);
                        }
                        else
                        {
                            ambientLight = 0.2f;
                        }
                    }

                }
            }
        }


        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButton.Left)
            {
                buttonLeftPressed = false;
                buttonRightPressed = false;
            }
        }

       protected override void OnMouseMove(MouseMoveEventArgs e)
       {
        base.OnMouseMove(e);

        if (e.Mouse.LeftButton == ButtonState.Pressed)
       {
        if (!isDragging)
        {
            isDragging = true;
            lastMousePos = new Vector2(e.X, e.Y);
        }
        else
        {
            Vector2 newMousePos = new Vector2(e.X, e.Y);
            Vector2 delta = newMousePos - lastMousePos;
            lastMousePos = newMousePos;

            cameraAngleX -= delta.Y * 0.3f;
            cameraAngleY += delta.X * 0.3f;

            // הגבלות כדי למנוע היפוך של העולם
            cameraAngleX = MathHelper.Clamp(cameraAngleX, 20f, 89f);
        }
       }
        else
        {
        isDragging = false;
        }
     }



        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            zoom += e.Delta * 0.5f;
        }

        private void DrawButtons()
        {
            GL.MatrixMode(MatrixMode.Projection);
            GL.PushMatrix();
            GL.LoadIdentity();
            GL.Ortho(0, Width, 0, Height, -1, 1);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.PushMatrix();
            GL.LoadIdentity();

            GL.Disable(EnableCap.DepthTest);

            int buttonSize = 60;
            int spacing = 10;
            int totalButtons = 6;
            int totalWidth = totalButtons * buttonSize + (totalButtons - 1) * spacing;
            int startX = (Width - totalWidth) / 2;
            int yStation = 50;

            for (int i = 0; i < 6; i++)
            {
                int wx = startX + i * (buttonSize + spacing);
                DrawTexturedButton(wx, yStation, buttonSize, buttonSize, btnWindmillTex[i]);
            }

            int centerX = Width / 2;
            int yControl = yStation + buttonSize + 20;
            DrawTexturedButton(centerX - 80, yControl, 70, 50, btnLeftTex);
            DrawTexturedButton(centerX + 10, yControl, 70, 50, btnRightTex);

            int sideX = Width - 90;
            int sideY = 60;
            DrawTexturedButton(sideX, sideY, 60, 60, btnStopTex);
            DrawTexturedButton(sideX, sideY + 70, 60, 60, btnStartTex);
            DrawTexturedButton(sideX, sideY + 140, 60, 60, btnSwitchTex);

            GL.Enable(EnableCap.DepthTest);
            GL.PopMatrix();
            GL.MatrixMode(MatrixMode.Projection);
            GL.PopMatrix();
            GL.MatrixMode(MatrixMode.Modelview);
        }

        private Matrix4 CreateShadowMatrix(Vector4 plane, Vector3 lightPos)
        {
            float dot = plane.X * lightPos.X + plane.Y * lightPos.Y + plane.Z * lightPos.Z + plane.W;

            Matrix4 mat = new Matrix4(
                dot - lightPos.X * plane.X, -lightPos.X * plane.Y, -lightPos.X * plane.Z, -lightPos.X * plane.W,
                -lightPos.Y * plane.X, dot - lightPos.Y * plane.Y, -lightPos.Y * plane.Z, -lightPos.Y * plane.W,
                -lightPos.Z * plane.X, -lightPos.Z * plane.Y, dot - lightPos.Z * plane.Z, -lightPos.Z * plane.W,
                -1 * plane.X, -1 * plane.Y, -1 * plane.Z, dot - 1 * plane.W
            );

            return mat;
        }
        private float[] CreateShadowMatrixFromPoints(float[,] points, float[] lightPosOriginal)
        {
            float[] shadowMatrix = new float[16];

            float length = (float)Math.Sqrt(
                lightPosOriginal[0] * lightPosOriginal[0] +
                lightPosOriginal[1] * lightPosOriginal[1] +
                lightPosOriginal[2] * lightPosOriginal[2]);
            if (length == 0) length = 1f;

            float[] L = new float[] {
        lightPosOriginal[0] / length,
        lightPosOriginal[1] / length,
        lightPosOriginal[2] / length,
        0.0f
    };

            float[] v1 = new float[3];
            float[] v2 = new float[3];
            for (int i = 0; i < 3; i++)
            {
                v1[i] = points[1, i] - points[0, i];
                v2[i] = points[2, i] - points[0, i];
            }

            float[] normal = new float[3];
            normal[0] = v1[1] * v2[2] - v1[2] * v2[1];
            normal[1] = v1[2] * v2[0] - v1[0] * v2[2];
            normal[2] = v1[0] * v2[1] - v1[1] * v2[0];

            float normLen = (float)Math.Sqrt(normal[0] * normal[0] + normal[1] * normal[1] + normal[2] * normal[2]);
            for (int i = 0; i < 3; i++) normal[i] /= normLen;

            float D = -(normal[0] * points[0, 0] + normal[1] * points[0, 1] + normal[2] * points[0, 2]);

            float[] plane = new float[4] { normal[0], normal[1], normal[2], D };

            float dot = plane[0] * L[0] + plane[1] * L[1] + plane[2] * L[2] + plane[3];

            for (int i = 0; i < 16; i++) shadowMatrix[i] = 0;

            shadowMatrix[0] = dot - L[0] * plane[0];
            shadowMatrix[4] = -L[0] * plane[1];
            shadowMatrix[8] = -L[0] * plane[2];
            shadowMatrix[12] = -L[0] * plane[3];

            shadowMatrix[1] = -L[1] * plane[0];
            shadowMatrix[5] = dot - L[1] * plane[1];
            shadowMatrix[9] = -L[1] * plane[2];
            shadowMatrix[13] = -L[1] * plane[3];

            shadowMatrix[2] = -L[2] * plane[0];
            shadowMatrix[6] = -L[2] * plane[1];
            shadowMatrix[10] = dot - L[2] * plane[2];
            shadowMatrix[14] = -L[2] * plane[3];

            shadowMatrix[3] = -L[3] * plane[0];
            shadowMatrix[7] = -L[3] * plane[1];
            shadowMatrix[11] = -L[3] * plane[2];
            shadowMatrix[15] = dot - L[3] * plane[3];

            return shadowMatrix;
        }



        private void DrawShadow(Vector3 position, float rotation, Vector3 sunDir)
        {
            if (sunDir.Y <= 0.1f) return;

            Vector4 groundPlane = new Vector4(0, 2, 0, -1);
            Matrix4 shadowMat = CreateShadowMatrix(groundPlane, sunDir);

            GL.PushMatrix();
            GL.MultMatrix(ref shadowMat);

            // תרגום התחנה לגובה הצל בלבד (אין +0.8)
            GL.Translate(position + new Vector3(0f, 1.2f, 0f));

            GL.Disable(EnableCap.Lighting);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.Color4(0.2f, 0.2f, 0.2f, 0.5f);

            DrawWindmillShadowModel(0, rotation, totalTime);
            GL.Disable(EnableCap.Blend);
            GL.Enable(EnableCap.Lighting);
            GL.PopMatrix();
        }

        void DrawWindmillShadowModel(int index, float rotation, double time)
        {
            GL.PushMatrix();

            // סיבוב התחנה סביב ציר Y בהתאם לזווית שהוגדרה
            GL.Rotate(rotation, 0, 1, 0);

            //  הדבקה פיזית + קיזוז קטן של הצל
            GL.Scale(0.01f, 1.01f, 1.5f); // הגדלה קטנה רק לצל כדי לסגור מרווחים

            // ציור העמוד
            GL.PushMatrix();
            GL.Translate(0f, 0f, 0f); // תחילת העמוד על הקרקע
            GL.Scale(0.15f, 3.9f, 0.15f); // ✅ מותאם לגובה הכדור (לא 3.1)
            GL.Begin(PrimitiveType.Quads);
            GL.Vertex3(-1, 0, 1); GL.Vertex3(1, 0, 1); GL.Vertex3(1, 1, 1); GL.Vertex3(-1, 1, 1);
            GL.Vertex3(-1, 0, -1); GL.Vertex3(1, 0, -1); GL.Vertex3(1, 1, -1); GL.Vertex3(-1, 1, -1);
            GL.Vertex3(-1, 0, -1); GL.Vertex3(-1, 0, 1); GL.Vertex3(-1, 1, 1); GL.Vertex3(-1, 1, -1);
            GL.Vertex3(1, 0, -1); GL.Vertex3(1, 0, 1); GL.Vertex3(1, 1, 1); GL.Vertex3(1, 1, -1);
            GL.End();
            GL.PopMatrix();

            // ציור הראש (כדור) – מוצמד לגובה העמוד
            GL.PushMatrix();
            GL.Translate(0f, 3.9f, 0f); // ✅ בדיוק מעל העמוד
            DrawSphere(Vector3.Zero, 0.248f, 8, 8);
            GL.PopMatrix();

            // ציור הפרופלור עם סיבוב
            GL.PushMatrix();
            GL.Translate(0f, 3.9f, 0f);      // ✅ גובה הראש
            GL.Translate(-0.8f, 0f, 0f);     // הזחה לצד
            GL.Rotate(90, 0, 1, 0);          // סיבוב ציר
            GL.Rotate(time * 100.0, 0, 0, 1); // ✅ סיבוב אנימציה

            for (int i = 0; i < 3; i++)
            {
                GL.Rotate(120, 0, 0, 1);
                GL.Begin(PrimitiveType.Quads);
                GL.Vertex3(0.0f, 0.0f, 0.0f);
                GL.Vertex3(0.5f, 0.05f, 0.0f);
                GL.Vertex3(1.5f, 0.05f, 0.0f);
                GL.Vertex3(1.5f, -0.05f, 0.0f);
                GL.Vertex3(0.5f, -0.05f, 0.0f);
                GL.End();
            }

            GL.PopMatrix(); // פרופלור
            GL.PopMatrix(); // התחנה כולה
        }


        // ✅ עדכון לפונקציה DrawLake עם שקיפות
        private void DrawLake()
        {
            GL.PushMatrix();
            GL.Translate(0f, 0.5f, 0f);
            GL.Scale(9f, 1f, 9f);
            GL.Begin(PrimitiveType.TriangleFan);
            GL.Color4(0.2f, 0.5f, 0.9f, 0.6f); // כחול שקוף
            GL.Vertex3(0f, 0f, 0f);
            int segments = 64;
            for (int i = 0; i <= segments; i++)
            {
                double angle = 2 * Math.PI * i / segments;
                float x = (float)Math.Cos(angle);
                float z = (float)Math.Sin(angle);
                GL.Vertex3(x, 0f, z);
            }
            GL.End();
            GL.PopMatrix();
        }


        private void DrawGrassBase()
        {
            int size = 20;
            float spacing = 0.1f;
            float lakeRadius = 9f;

            for (float x = -size; x < size; x += spacing)
            {
                for (float z = -size; z < size; z += spacing)
                {
                    float dist = (float)Math.Sqrt(x * x + z * z);

                    if (dist < lakeRadius) continue; // בתוך האגם – לא מציירים

                    GL.Begin(PrimitiveType.Quads);
                    GL.Color3(0.05f * ambientLight, 0.3f * ambientLight, 0.05f * ambientLight); // ירוק כהה

                    GL.Vertex3(x, 0f, z);
                    GL.Vertex3(x + spacing, 0f, z);
                    GL.Vertex3(x + spacing, 0f, z + spacing);
                    GL.Vertex3(x, 0f, z + spacing);

                    GL.End();
                }
            }
        }

        

        private void DrawGrassField(double time)
        {
            int size = 20;
            float spacing = 0.2f;

            for (float x = -size; x < size; x += spacing)
            {
                for (float z = -size; z < size; z += spacing)
                {
                    float dist = (float)Math.Sqrt(x * x + z * z);
                    if (dist < 9f) // ⬅️ בתוך האגם? מדלגים
                        continue;

                    float sway = 0f;
                    Vector3 basePos = new Vector3(x, 0f, z);
                    Vector3 tipPos = new Vector3(x + sway, 1.0f, z);

                    GL.Begin(PrimitiveType.Lines);
                    GL.Color3(0.1f * ambientLight, 0.6f * ambientLight, 0.1f * ambientLight);
                    GL.Vertex3(basePos);
                    GL.Color3(0.2f * ambientLight, 0.8f * ambientLight, 0.2f * ambientLight);
                    GL.Vertex3(tipPos);
                    GL.End();
                }
            }
        }


        private Vector3 GetWindmillPosition(int index)
        {
            float radius = 12f; // ⬅️ רדיוס גדול מהאגם (9) לשולי בטיחות
            float angleDeg = index * (360f / 5); // 5 תחנות – חלוקה שווה
            float angleRad = MathHelper.DegreesToRadians(angleDeg);

            float x = (float)Math.Cos(angleRad) * radius;
            float z = (float)Math.Sin(angleRad) * radius;

            return new Vector3(x, 0f, z);
        }


        private void DrawWindmill(double time, Vector3 position, float rotation)
        {
            // מגדל התחנה
            GL.PushMatrix();
            GL.Translate(position + new Vector3(0f, 1.0f, 0f)); // בסיס התחנה
            GL.Translate(0f, 2.5f, 0f); // ⬅️ מרכז העמוד – כדי שיגיע בדיוק עד 3.9
            GL.Scale(0.15f, -2.5f, 0.15f); // ⬅️ גובה העמוד: 3.1 יחידות

            GL.Begin(PrimitiveType.Quads);
            GL.Color3(0.6f, 0.6f, 0.6f); // אפור קבוע
            GL.Vertex3(-1, 0, 1); GL.Vertex3(1, 0, 1); GL.Vertex3(1, 1, 1); GL.Vertex3(-1, 1, 1);
            GL.Vertex3(-1, 0, -1); GL.Vertex3(1, 0, -1); GL.Vertex3(1, 1, -1); GL.Vertex3(-1, 1, -1);
            GL.Vertex3(-1, 0, -1); GL.Vertex3(-1, 0, 1); GL.Vertex3(-1, 1, 1); GL.Vertex3(-1, 1, -1);
            GL.Vertex3(1, 0, -1); GL.Vertex3(1, 0, 1); GL.Vertex3(1, 1, 1); GL.Vertex3(1, 1, -1);
            GL.End();
            GL.PopMatrix();

            // ראש ופרופלור
            Vector3 spherePosition = position + new Vector3(0f, 1f + 2.5f + headHeightOffset, 0f);
            GL.PushMatrix();
            GL.Translate(spherePosition);
            GL.Rotate(rotation, 0, 1, 0);

            GL.Color3(0.8f, 0.8f, 0.8f);
            DrawSphere(Vector3.Zero, 0.248f, 16, 16);

            GL.Color3(0.7f, 0.7f, 0.7f);
            DrawCone(new Vector3(-0.8f, 0f, 0f), 0.6f, 0.2f, 20);

            GL.PushMatrix();
            GL.Translate(new Vector3(-0.8f, 0f, 0f));
            GL.Rotate(90, 0, 1, 0);
            GL.Rotate(time * 100.0, 0, 0, 1);

            for (int i = 0; i < 3; i++)
            {
                GL.Rotate(120, 0, 0, 1);
                GL.Begin(PrimitiveType.Quads);
                GL.Color3(1.0f, 1.0f, 1.0f);
                float lengthStart = 0.08f;
                float lengthEnd = 1.7f;
                float height = 0.08f;        // על ציר Y – רוחב הלהב
                float thickness = 0.15f;     // ✅ על ציר Z – עובי ממשי

                float zFront = -thickness / 2f;
                float zBack = thickness / 2f;

                GL.Begin(PrimitiveType.Quads);

                // פאה קדמית
                GL.Vertex3(lengthStart, -height, zFront);
                GL.Vertex3(lengthEnd, -height, zFront);
                GL.Vertex3(lengthEnd, height, zFront);
                GL.Vertex3(lengthStart, height, zFront);

                // פאה אחורית
                GL.Vertex3(lengthStart, -height, zBack);
                GL.Vertex3(lengthEnd, -height, zBack);
                GL.Vertex3(lengthEnd, height, zBack);
                GL.Vertex3(lengthStart, height, zBack);

                // עליונה
                GL.Vertex3(lengthStart, height, zFront);
                GL.Vertex3(lengthEnd, height, zFront);
                GL.Vertex3(lengthEnd, height, zBack);
                GL.Vertex3(lengthStart, height, zBack);

                // תחתונה
                GL.Vertex3(lengthStart, -height, zFront);
                GL.Vertex3(lengthEnd, -height, zFront);
                GL.Vertex3(lengthEnd, -height, zBack);
                GL.Vertex3(lengthStart, -height, zBack);

                // צד ימין
                GL.Vertex3(lengthEnd, -height, zFront);
                GL.Vertex3(lengthEnd, height, zFront);
                GL.Vertex3(lengthEnd, height, zBack);
                GL.Vertex3(lengthEnd, -height, zBack);

                // צד שמאל
                GL.Vertex3(lengthStart, -height, zFront);
                GL.Vertex3(lengthStart, height, zFront);
                GL.Vertex3(lengthStart, height, zBack);
                GL.Vertex3(lengthStart, -height, zBack);

                GL.End();

            }

            GL.PopMatrix(); // פרופלור
            GL.PopMatrix(); // ראש
        }


        private void DrawSphere(Vector3 position, float radius, int slices, int stacks)
        {
            GL.PushMatrix();
            GL.Translate(position);
            for (int i = 0; i <= stacks; i++)
            {
                double lat0 = Math.PI * (-0.5 + (double)(i - 1) / stacks);
                double z0 = Math.Sin(lat0);
                double zr0 = Math.Cos(lat0);

                double lat1 = Math.PI * (-0.5 + (double)i / stacks);
                double z1 = Math.Sin(lat1);
                double zr1 = Math.Cos(lat1);

                GL.Begin(PrimitiveType.QuadStrip);
                for (int j = 0; j <= slices; j++)
                {
                    double lng = 2 * Math.PI * (double)(j - 1) / slices;
                    double x = Math.Cos(lng);
                    double y = Math.Sin(lng);

                    GL.Vertex3(x * zr0 * radius, y * zr0 * radius, z0 * radius);
                    GL.Vertex3(x * zr1 * radius, y * zr1 * radius, z1 * radius);
                }
                GL.End();
            }
            GL.PopMatrix();
        }

       private void DrawCone(Vector3 position, float height, float baseRadius, int segments, Color4? overrideColor = null)
{
    GL.PushMatrix();
    GL.Translate(position);
    GL.Rotate(90, 0, 1, 0);

    if (overrideColor.HasValue)
        GL.Color4(overrideColor.Value);
    else
        GL.Color3(1.0f, 0.5f, 0.0f); // כתום רגיל

    GL.Begin(PrimitiveType.TriangleFan);
    GL.Vertex3(0, 0, 0);

    for (int i = 0; i <= segments; i++)
    {
        double angle = 2 * Math.PI * i / segments;
        double x = Math.Cos(angle) * baseRadius;
        double y = Math.Sin(angle) * baseRadius;
        GL.Vertex3(x, y, height);
    }

    GL.End();
    GL.PopMatrix();
}



        private void DrawStars(bool even)
        {
            GL.PointSize(2f);
            GL.Begin(PrimitiveType.Points);
            for (int i = 0; i < stars.Length; i++)
            {
                if ((i % 2 == 0) == even)
                {
                    float twinkle = 0.8f + 0.2f * (float)Math.Sin(totalTime * 5 + i);
                    GL.Color3(twinkle, twinkle, twinkle);
                    GL.Vertex3(stars[i]);
                }
            }
            GL.End();
        }

        private void DrawSunAndMoon()
        {
            GL.PushMatrix();
            GL.Disable(EnableCap.Lighting);

            Vector3 sunPos = new Vector3(
                (float)Math.Cos(MathHelper.DegreesToRadians(sunAngle)) * 21f,
                (float)Math.Sin(MathHelper.DegreesToRadians(sunAngle)) * 15f,
                0f);

            Vector3 moonPos = new Vector3(
                (float)Math.Cos(MathHelper.DegreesToRadians(moonAngle)) * 21f,
                (float)Math.Sin(MathHelper.DegreesToRadians(moonAngle)) * 15f,
                0f);

            if (sunAngle <= 180f)
            {
                GL.PushMatrix();
                GL.Color3(1.0f, 1.0f, 0.0f);
                GL.Translate(sunPos);
                DrawSphere(Vector3.Zero, 1.0f, 16, 16);
                GL.PopMatrix();
            }
            else if (sunAngle > 180f)
            {
                GL.PushMatrix();
                GL.Color3(1.0f, 1.0f, 1.0f);
                GL.Translate(moonPos);
                DrawSphere(Vector3.Zero, 1.0f, 16, 16);
                GL.PopMatrix();
            }

            GL.PopMatrix();
        }
    }
}