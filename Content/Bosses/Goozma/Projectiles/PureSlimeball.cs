﻿using CalamityHunt.Common.Systems.Particles;
using CalamityHunt.Content.Particles;
using Humanizer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityHunt.Content.Bosses.Goozma.Projectiles
{
    public class PureSlimeball : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 60;
            Projectile.height = 60;
            Projectile.tileCollide = false;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.aiStyle = -1;
        }

        public override void OnSpawn(IEntitySource source)
        {
            Projectile.localAI[1] = Main.rand.NextFloat(30f);
            Projectile.rotation = Main.rand.NextFloat(-1f, 1f);

            if (!Main.dedServ)
            {
                SoundStyle shootSound = new SoundStyle($"{nameof(CalamityHunt)}/Assets/Sounds/Goozma/GoozmaShot", 1, 2);
                shootSound.Pitch = -0.3f;
                shootSound.MaxInstances = 0;

                SoundEngine.PlaySound(shootSound, Projectile.Center);
                for (int i = 0; i < 5; i++)
                {
                    Color glowColor = new GradientColor(SlimeUtils.GoozColorArray, 0.4f, 0.5f).ValueAt(Projectile.localAI[1]);
                    glowColor.A /= 2;
                    Dust.NewDustPerfect(Projectile.Center, DustID.RainbowMk2, Projectile.velocity + Main.rand.NextVector2Circular(5, 5), 0, glowColor, 1.5f).noGravity = true;
                }
            }
        }

        public ref float Time => ref Projectile.ai[0];

        public override void AI()
        {
            if (Projectile.ai[1] == 0)
            {
                if (Time > 8)
                    Projectile.velocity *= 0.955f;

                Projectile.rotation = (float)Math.Sin(Projectile.localAI[1] * 0.03f) * Projectile.direction * 0.1f + Projectile.velocity.X * 0.1f;
                Projectile.scale = (float)Math.Sqrt(Utils.GetLerpValue(0, 17, Projectile.localAI[0], true)) + (float)Math.Pow(Utils.GetLerpValue(97, 100, Time, true), 2f) * 0.5f;

                int target = -1;
                if (Main.player.Any(n => n.active && !n.dead))
                    target = Main.player.First(n => n.active && !n.dead).whoAmI;

                if (target > -1)
                    Projectile.velocity += Projectile.DirectionTo(Main.player[target].MountedCenter).SafeNormalize(Vector2.Zero).RotatedByRandom(0.2f) * 0.01f * (float)Math.Pow(Math.Sin(Time * 0.05f), 2f);

                if (Main.rand.NextBool(8))
                {
                    Particle hue = Particle.NewParticle(Particle.ParticleType<HueLightDust>(), Projectile.Center + Main.rand.NextVector2Circular(40, 40), -Vector2.UnitY * Main.rand.NextFloat(2f), Color.White, 1.2f);
                    hue.data = Projectile.localAI[1];
                }

                Projectile.frameCounter++;
                if (Projectile.frameCounter > 7)
                {
                    Projectile.frame = (Projectile.frame + 1) % 4;
                    Projectile.frameCounter = 0;
                }

                if (Time > 100)
                {
                    Time = 0;
                    Projectile.ai[1]++;
                }
                if (Time == 2 && !Main.dedServ)
                {
                    SoundStyle chargeSound = new SoundStyle($"{nameof(CalamityHunt)}/Assets/Sounds/Goozma/GoozmaPureBallCharge");
                    chargeSound.MaxInstances = 0;
                    SoundEngine.PlaySound(chargeSound, Projectile.Center);
                }
            }
            else
            {
                Projectile.scale = 1f;

                if (Time == 1)
                {
                    SoundStyle explodeSound = new SoundStyle($"{nameof(CalamityHunt)}/Assets/Sounds/Goozma/GoozmaBloatedBlastShoot");
                    explodeSound.MaxInstances = 0;
                    SoundEngine.PlaySound(explodeSound, Projectile.Center);
                    SoundEngine.PlaySound(SoundID.DD2_KoboldExplosion.WithVolumeScale(0.6f), Projectile.Center);
                }

                if (Time < 10)
                {
                    Particle hue = Particle.NewParticle(Particle.ParticleType<HueLightDust>(), Projectile.Center + Main.rand.NextVector2Circular(20, 20), Main.rand.NextVector2Circular(25, 25), Color.White, 1.5f);
                    hue.data = Projectile.localAI[1];

                    for (int i = 0; i < 10; i++)
                        Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(20, 20), DustID.TintableDust, Main.rand.NextVector2Circular(10, 10), 100, Color.Black, 1f + Main.rand.NextFloat(2)).noGravity = true;
                }

                if (Time > 60)
                    Projectile.Kill();
            }

            Time++;
            Projectile.localAI[0]++;
            Projectile.localAI[1]++;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float maxDist = Math.Min(Projectile.Distance(targetHitbox.Center()), 250);
            //Dust.QuickDust(Projectile.Center + new Vector2(maxDist, 0).RotatedBy(Projectile.Center.AngleTo(targetHitbox.Center())), Color.Red);

            if (Projectile.ai[1] == 1)
            {
                Vector2 radius = Projectile.Center + new Vector2(maxDist, 0).RotatedBy(Projectile.Center.AngleTo(targetHitbox.Center()));
                if (Time < 25)
                    return targetHitbox.Contains(radius.ToPoint());
                return false;
            }
            else
                return projHitbox.Intersects(targetHitbox);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Asset<Texture2D> texture = ModContent.Request<Texture2D>(Texture);
            Asset<Texture2D> glow = ModContent.Request<Texture2D>($"{nameof(CalamityHunt)}/Assets/Textures/Goozma/GlowSoft");
            Asset<Texture2D> ring = ModContent.Request<Texture2D>($"{nameof(CalamityHunt)}/Assets/Textures/Goozma/GlowRing");
            Rectangle baseFrame = texture.Frame(3, 4, 0, Projectile.frame);
            Rectangle glowFrame = texture.Frame(3, 4, 1, Projectile.frame);
            Rectangle outlineFrame = texture.Frame(3, 4, 2, Projectile.frame);

            Color bloomColor = new GradientColor(SlimeUtils.GoozColorArray, 0.2f, 0.2f).ValueAt(Projectile.localAI[1]);
            bloomColor.A = 0;

            if (Projectile.ai[1] == 0)
            {
                float ringScale = (float)Math.Cbrt(Utils.GetLerpValue(0, 100, Time, true));
                float ringPower = 1f + (float)Math.Sin(Math.Pow(Time * 0.027f, 3f)) * 0.4f;

                Main.EntitySpriteDraw(glow.Value, Projectile.Center - Main.screenPosition, null, bloomColor * 0.3f * ringScale * ringPower, Projectile.rotation, glow.Size() * 0.5f, (250f / glow.Height() * 2f + 3f) * ringScale, 0, 0);
                Main.EntitySpriteDraw(glow.Value, Projectile.Center - Main.screenPosition, null, bloomColor * 0.4f * ringScale * ringPower, 0, glow.Size() * 0.5f, (250f / glow.Height() * 2f) * 0.5f * ringScale, 0, 0);
                Main.EntitySpriteDraw(ring.Value, Projectile.Center - Main.screenPosition, null, bloomColor * 0.1f * ringScale * ringPower, Projectile.rotation * 0.1f, ring.Size() * 0.5f, (250f / ring.Height() * 2f + 0.5f) * ringScale, 0, 0);

                Main.EntitySpriteDraw(texture.Value, Projectile.Center - Main.screenPosition, baseFrame, lightColor, Projectile.rotation, baseFrame.Size() * 0.5f, Projectile.scale, 0, 0);
                Main.EntitySpriteDraw(texture.Value, Projectile.Center - Main.screenPosition, glowFrame, bloomColor, Projectile.rotation, glowFrame.Size() * 0.5f, Projectile.scale, 0, 0);
                Main.EntitySpriteDraw(texture.Value, Projectile.Center - Main.screenPosition, glowFrame, bloomColor * 0.8f, Projectile.rotation, glowFrame.Size() * 0.5f, Projectile.scale * 1.05f, 0, 0);
                Main.EntitySpriteDraw(glow.Value, Projectile.Center - Main.screenPosition, null, bloomColor * 0.4f, Projectile.rotation, glow.Size() * 0.5f, Projectile.scale * 2f, 0, 0);

            }
            else
            {
                float ringScale = 1.01f + (float)Math.Sqrt(Time / 60f) * 0.3f;
                float ringPower = (float)Math.Pow(Utils.GetLerpValue(30, 0, Time, true), 3f);

                Main.EntitySpriteDraw(glow.Value, Projectile.Center - Main.screenPosition, null, bloomColor * 0.2f * Utils.GetLerpValue(30, 0, Time, true), Projectile.rotation, glow.Size() * 0.5f, (250f / glow.Height() * 2f + 5f) * ringScale * ringPower, 0, 0);
                Main.EntitySpriteDraw(glow.Value, Projectile.Center - Main.screenPosition, null, bloomColor * 0.5f * Utils.GetLerpValue(20, 0, Time, true), 0, glow.Size() * 0.5f, (250f / glow.Height() * 2f) * 0.6f * ringScale * ringPower, 0, 0);
                Main.EntitySpriteDraw(ring.Value, Projectile.Center - Main.screenPosition, null, bloomColor * 0.1f * ringPower, Projectile.rotation * 0.1f, ring.Size() * 0.5f, (250f / ring.Height() * 2f + 0.5f) * ringScale, 0, 0);
            }

            return false;
        }
    }
}
