﻿using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Quaver.GameState;
using Quaver.Graphics;
using Quaver.Graphics.Base;
using Quaver.Graphics.Sprites;
using Quaver.Graphics.Text;
using Quaver.Helpers;
using Quaver.Main;

namespace Quaver.States.Gameplay.Mania.UI.Playfield
{
    internal class ManiaPlayfieldUI : IGameStateComponent
    {
        /// <summary>
        ///     The parent of every ManiaPlayfield QuaverUserInterface Component
        /// </summary>
        public Container Container { get; private set; }

        /// <summary>
        ///     This displays the judging (MARV/PERF/GREAT/ect)
        /// </summary>
        private Sprite JudgeSprite { get; set; }

        /// <summary>
        ///     The sprite for every Offset Indicator bar
        /// </summary>
        private Sprite[] OffsetIndicatorsSprites { get; set; }

        /// <summary>
        ///     Bar images which display the player's current multiplier
        /// </summary>
        private Sprite[] MultiplierBars { get; set; }

        /// <summary>
        ///     The player's health bar
        /// </summary>
        private Sprite HealthBarOver { get; set; }

        /// <summary>
        ///     Used to reference the images for JudgeQuaverSprite
        /// </summary>
        private Texture2D[] JudgeImages { get; set; }

        /// <summary>
        ///     Reference to the size each judge image is
        /// </summary>
        private Vector2[] JudgeSizes { get; set; }

        /// <summary>
        ///     The text displaying combo
        /// </summary>
        private SpriteText ComboText { get; set; }

        /// <summary>
        ///     When the JudgeQuaverSprite gets updated, it'll update JudgeQuaverSprite.PositionY to this variable.
        /// </summary>
        private float JudgeHitOffset { get; set; }

        /// <summary>
        ///     The judge image that has priority other judge imaages that is displayed. Worse judgement has more priority (MISS > BAD > OKAY... ect)
        /// </summary>
        private int PriorityJudgeImage { get; set; } = 0;

        /// <summary>
        ///     How long the prioritized judge image will be displayed for
        /// </summary>
        private double PriorityJudgeLength { get; set; }

        /// <summary>
        ///     The bars which indicate how off players are from the receptor
        /// </summary>
        private const int OffsetIndicatorSize = 32;

        /// <summary>
        ///     The size of the Offset Gauage
        /// </summary>
        private float OffsetGaugeSize { get; set; }

        /// <summary>
        ///     Index of the current Offset Indicator. It will cycle through whatever the indicator size is to pool the indicator sprites
        /// </summary>
        private int CurrentOffsetObjectIndex { get; set; }

        /// <summary>
        ///     The alpha of the entire QuaverUserInterface set. Will turn invisible if the set is not being updated.
        /// </summary>
        private double SpriteAlphaHold { get; set; }

        /// <summary>
        ///     Total number of multiplier bars which are active
        /// </summary>
        private int ActiveMultiplierBars { get; set; }

        internal float PlayfieldSize { get; set; }

        public void Draw()
        {
            Container.Draw();
        }

        public void Initialize(IGameState state)
        {
            // Reference Variables
            SpriteAlphaHold = 0;
            CurrentOffsetObjectIndex = 0;
            ActiveMultiplierBars = 0;

            // Create Judge QuaverSprite/References
            JudgeImages = new Texture2D[6]
            {
                GameBase.LoadedSkin.JudgeMarv[0],
                GameBase.LoadedSkin.JudgePerf[0],
                GameBase.LoadedSkin.JudgeGreat[0],
                GameBase.LoadedSkin.JudgeGood[0],
                GameBase.LoadedSkin.JudgeOkay[0],
                GameBase.LoadedSkin.JudgeMiss[0]
            };

            JudgeSizes = new Vector2[6];
            for (var i = 0; i < 6; i++)
            {
                //todo: replace 40 with skin.ini value
                JudgeSizes[i] = new Vector2(JudgeImages[i].Width, JudgeImages[i].Height) * 40f * GameBase.WindowUIScale / JudgeImages[i].Height;
            }
            JudgeHitOffset = -5f * GameBase.WindowUIScale;

            // Create QuaverContainer
            Container = new Container()
            {
                Size = new UDim2D(PlayfieldSize, 0, 0, 1),
                Alignment = Alignment.MidCenter
            };

            // TODO: add judge scale
            JudgeSprite = new Sprite()
            {
                Size = new UDim2D(JudgeSizes[0].X, JudgeSizes[0].Y),
                Alignment = Alignment.MidCenter,
                Image = JudgeImages[0],
                Parent = Container,
                Alpha = 0
            };

            // Create Combo Text
            ComboText = new SpriteText()
            {
                Size = new UDim2D(100 * GameBase.WindowUIScale, 20 * GameBase.WindowUIScale),
                Position = new UDim2D(0, 45 * GameBase.WindowUIScale),
                Alignment = Alignment.MidCenter,
                TextAlignment = Alignment.TopCenter,
                Text = "0x",
                TextScale = GameBase.WindowUIScale,
                Font = QuaverFonts.Medium16,
                Parent = Container,
                Alpha = 0
            };

            // Create Offset Gauge
            var offsetGaugeBoundary = new Container()
            {
                Size = new UDim2D(220 * GameBase.WindowUIScale, 10 * GameBase.WindowUIScale),
                Position = new UDim2D(0, 30 * GameBase.WindowUIScale),
                Alignment = Alignment.MidCenter,
                Parent = Container
            };

            //todo: offsetGaugeBoundary.SizeX with a new size. Right now the offset gauge is the same size as the hitwindow
            //OffsetGaugeSize = offsetGaugeBoundary.SizeX / (ManiaGameplayReferences.PressWindowLatest * 2 * GameBase.WindowUIScale);
            OffsetGaugeSize = offsetGaugeBoundary.SizeX / (200 * GameBase.WindowUIScale);

            OffsetIndicatorsSprites = new Sprite[OffsetIndicatorSize];
            for (var i = 0; i < OffsetIndicatorSize; i++)
            {
                OffsetIndicatorsSprites[i] = new Sprite()
                {
                    Parent = offsetGaugeBoundary,
                    Size = new UDim2D(4, 0, 0, 1),
                    Alignment = Alignment.MidCenter,
                    Alpha = 0
                };
            }

            var offsetGaugeMiddle = new Sprite()
            {
                Size = new UDim2D(2, 0, 0, 1),
                Alignment = Alignment.MidCenter,
                Parent = offsetGaugeBoundary
            };

            // Create Health Bar
            var healthMultiplierBoundary = new Container()
            {
                Size = new UDim2D(PlayfieldSize - 4, 20 * GameBase.WindowUIScale),
                PosY = Config.ConfigManager.HealthBarPositionTop.Value ? 2 : -2,
                Alignment = Config.ConfigManager.HealthBarPositionTop.Value ? Alignment.TopCenter : Alignment.BotCenter,
                Parent = Container
            };

            var healthBarUnder = new Sprite()
            {
                Size = new UDim2D(0, 10 * GameBase.WindowUIScale -1, 1, 0),
                Alignment = Config.ConfigManager.HealthBarPositionTop.Value ? Alignment.TopCenter : Alignment.BotCenter,
                Parent = healthMultiplierBoundary
            };

            HealthBarOver = new Sprite()
            {
                Size = new UDim2D(-2, -2, 1, 1),
                PosX = 1,
                Parent = healthBarUnder,
                Alignment = Alignment.MidLeft,
                Tint = Color.Green
            };

            // Create Multiplier Bars
            MultiplierBars = new Sprite[15];
            for (var i = 0; i < 15; i++)
            {
                MultiplierBars[i] = new Sprite()
                {
                    Size = new UDim2D(14 * GameBase.WindowUIScale, 10 * GameBase.WindowUIScale -1),
                    PosX = (i-7.5f) * 16 * GameBase.WindowUIScale,
                    PosY = Config.ConfigManager.HealthBarPositionTop.Value ? 10 * GameBase.WindowUIScale + 1 : 0,
                    Alignment = Alignment.TopCenter,
                    Image = GameBase.QuaverUserInterface.HollowBox,
                    Parent = healthMultiplierBoundary
                };
            }
        }

        public void UnloadContent()
        {
            Container.Destroy();
        }

        public void Update(double dt)
        {
            // Update the delta time tweening variable for animation.
            SpriteAlphaHold += dt;
            PriorityJudgeLength -= dt;
            if (PriorityJudgeLength <= 0)
            {
                PriorityJudgeLength = 0;
                PriorityJudgeImage = 0;
            }
            var tween = Math.Min(dt / 30, 1);

            // Update Offset Indicators
            foreach (var sprite in OffsetIndicatorsSprites)
            {
                sprite.Alpha = GraphicsHelper.Tween(0, sprite.Alpha, tween / 30);
            }

            // Update Judge Alpha
            JudgeSprite.PosY = GraphicsHelper.Tween(0, JudgeSprite.PosY, tween / 2);
            if (SpriteAlphaHold > 500 && PriorityJudgeLength <= 0)
            {
                JudgeSprite.Alpha = GraphicsHelper.Tween(0, JudgeSprite.Alpha, tween / 10);
                ComboText.Alpha = GraphicsHelper.Tween(0, ComboText.Alpha, tween / 10);
            }

            //Update QuaverContainer
            Container.Update(dt);
        }

        /// <summary>
        ///     Update Judge Image and Combo/Note ms offset
        /// </summary>
        /// <param name="index"></param>
        /// <param name="combo"></param>
        /// <param name="release"></param>
        /// <param name="offset"></param>
        internal void UpdateJudge(int index, int combo, bool release = false, double? offset = null)
        {
            //TODO: add judge scale
            ComboText.Text = combo + "x";
            ComboText.Alpha = 1;
            JudgeSprite.Alpha = 1;
            SpriteAlphaHold = 0;

            if (index >= PriorityJudgeImage || PriorityJudgeLength <= 0)
            {
                // Priority Judge Image to show
                if (index < 2) PriorityJudgeLength = 10;
                else if (index == 2) PriorityJudgeLength = 50;
                else if (index == 3) PriorityJudgeLength = 100;
                else PriorityJudgeLength = 500;
                PriorityJudgeImage = index;

                // Update judge sprite
                JudgeSprite.SizeX = JudgeSizes[index].X;
                JudgeSprite.SizeY = JudgeSizes[index].Y;
                JudgeSprite.Image = JudgeImages[index];
                JudgeSprite.PosY = JudgeHitOffset;
                JudgeSprite.Update(0);
            }

            if (index != 5 && !release && offset != null)
            {
                CurrentOffsetObjectIndex++;
                if (CurrentOffsetObjectIndex >= OffsetIndicatorSize) CurrentOffsetObjectIndex = 0;
                OffsetIndicatorsSprites[CurrentOffsetObjectIndex].Tint = GameBase.LoadedSkin.JudgeColors[index];
                OffsetIndicatorsSprites[CurrentOffsetObjectIndex].PosX = -(float)offset * OffsetGaugeSize;
                OffsetIndicatorsSprites[CurrentOffsetObjectIndex].Alpha = 0.5f;
                OffsetIndicatorsSprites[CurrentOffsetObjectIndex].Update(0);
            }
        }

        /// <summary>
        ///     Update Multiplier bars
        ///     todo: create cool fx
        /// </summary>
        /// <param name="total"></param>
        internal void UpdateMultiplierBars(int total)
        {
            //total should be between or equal to 0 and 15
            if (total > 15 || total < 0) return;

            // If a new bar turns active, do fx and stuff
            if (total > ActiveMultiplierBars)
            {
                ActiveMultiplierBars = total;
                MultiplierBars[total-1].Image = GameBase.QuaverUserInterface.BlankBox;
            }
            // If a new bar turns inactive
            else if (total < ActiveMultiplierBars)
            {
                ActiveMultiplierBars = total;
                for (var i = 1; i <= 15; i++)
                {
                    if (i > total)
                        MultiplierBars[i-1].Image = GameBase.QuaverUserInterface.HollowBox;
                    else
                        MultiplierBars[i-1].Image = GameBase.QuaverUserInterface.BlankBox;
                }
            }
        }

        /// <summary>
        ///     Update the health bar
        /// </summary>
        /// <param name="health"></param>
        internal void UpdateHealthBar(double health)
        {
            HealthBarOver.ScaleX = (float)health / 100;
        }
    }
}
