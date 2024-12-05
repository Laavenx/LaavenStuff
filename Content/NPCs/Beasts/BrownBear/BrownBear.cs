using Microsoft.Xna.Framework;
using System;
using LaavensStuff.Utils;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace LaavensStuff.Content.NPCs.Beasts.BrownBear;

public class BrownBear : ModNPC
{
    private enum ActionState
    {
        Asleep,
        Notice,
        Jump,
        Fall,
        KnockedBack
    }

    private enum Frame
    {
        Asleep,
        Falling = 5,
    }

    public ref float AI_State => ref NPC.ai[0];
    public ref float AI_Timer => ref NPC.ai[1];

    private const float NoticeDistance = 900f;
    private const float ForgetDistance = 1100f;
    private const float MaxSpeed = 2.75f;

    public override void SetStaticDefaults()
    {
        Main.npcFrameCount[NPC.type] = 6;

        NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Frostburn] = true;
        NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Frostburn2] = true;
    }

    public override void SetDefaults()
    {
        NPC.width = 100;
        NPC.height = 68;
        NPC.aiStyle = -1;
        NPC.lifeMax = 1000;
        NPC.defense = 45;
        NPC.damage = 70;
        NPC.HitSound = SoundID.NPCHit56;
        NPC.DeathSound = SoundID.NPCDeath5;
        NPC.value = 300;

        if (Main.expertMode)
        {
            NPC.knockBackResist = 0.35f;
        } else if (Main.masterMode)
        {
            NPC.knockBackResist = 0.3f;
        }
        else
        {
            NPC.knockBackResist = 0.4f;
        }
    }

    public override float SpawnChance(NPCSpawnInfo spawnInfo)
    {
        bool isOverworld = spawnInfo.Player.ZoneDirtLayerHeight || spawnInfo.Player.ZoneOverworldHeight;
        if (Main.hardMode && isOverworld)
        {
            return 0.01f;
        }

        return 0;
    }

    public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
    {
        if (hurtInfo.Damage > 0 && Main.rand.Next(3) == 0)
            target.AddBuff(BuffID.Bleeding, 2400, true);
    }

    public override void ModifyNPCLoot(NPCLoot npcLoot)
    {
        npcLoot.Add(ItemDropRule.Common(ItemID.BottledHoney, 2));
    }

    public override void AI()
    {
        if (NPC.justHit)
        {
            AI_State = (float)ActionState.KnockedBack;
            AI_Timer = 0;
        }

        switch (AI_State)
        {
            case (float)ActionState.Asleep:
                FallAsleep();
                break;
            case (float)ActionState.Notice:
                Notice();
                break;
            case (float)ActionState.Jump:
                Jump();
                break;
            case(float)ActionState.Fall:
                Fall();
                break;
            case(float)ActionState.KnockedBack:
                KnockedBack();
                break;
        }
    }

    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        SpriteEffects spriteEffects = SpriteEffects.None;
        if (NPC.spriteDirection == 1)
            spriteEffects = SpriteEffects.FlipHorizontally;
        
        Texture2D texture = TextureAssets.Npc[Type].Value;
        
        int frameHeight = texture.Height / Main.npcFrameCount[Type];
        int startY = NPC.frame.Y;
        
        Rectangle sourceRectangle = new Rectangle(0, startY, texture.Width, frameHeight);
        Vector2 origin = sourceRectangle.Size() / 2f;
        
        float offsetX = -12f;
        origin.X += (float)(NPC.spriteDirection == 1 ? offsetX : -offsetX);

        // Applying lighting and draw current frame
        //drawColor = Projectile.GetAlpha(lightColor);
        Main.EntitySpriteDraw(texture,
            NPC.Center - Main.screenPosition + new Vector2(0f, NPC.gfxOffY),
            sourceRectangle, drawColor, NPC.rotation, origin, NPC.scale, spriteEffects, 0);
        
        return false;
    }

    public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        base.PostDraw(spriteBatch, screenPos, drawColor);
        
        // Rectangle extraDamageHitbox = new Rectangle((int)NPC.position.X, (int)NPC.position.Y + 18, NPC.width - 10,
        //     NPC.height - 18);
        
        //GeneralUtils.DrawBorderedRect(Main.spriteBatch, Color.Red, Color.White, new Vector2(NPC.position.X, NPC.position.Y + 26), new Vector2(NPC.width - 10, NPC.height - 18), 0);
    }
    
    public override void FindFrame(int frameHeight)
    {
        NPC.spriteDirection = NPC.direction;

        switch (AI_State)
        {
            case (float)ActionState.Asleep:
                NPC.frameCounter = 0;
                NPC.frame.Y = (int)Frame.Asleep * frameHeight;
                break;
            case (float)ActionState.Notice:
                if (AI_Timer % Math.Round(9 / float.Abs(NPC.velocity.X)) == 0)
                {
                    NPC.frameCounter++;
                    NPC.frame.Y = (int)NPC.frameCounter * frameHeight;

                    if (NPC.frameCounter > 5)
                    {
                        NPC.frameCounter = 0;
                    }

                    NPC.frame.Y = (int)NPC.frameCounter * frameHeight;
                }

                break;
            case (float)ActionState.Jump:
                NPC.frameCounter = (int)Frame.Falling;
                NPC.frame.Y = (int)Frame.Falling * frameHeight;
                break;
            case (float)ActionState.Fall:
                NPC.frameCounter = (int)Frame.Falling;
                NPC.frame.Y = (int)Frame.Falling * frameHeight;
                break;
        }
    }

    private void FallAsleep()
    {
        NPC.TargetClosest(false);

        if (NPC.HasValidTarget && Main.player[NPC.target].Distance(NPC.Center) < NoticeDistance)
        {
            NPC.FaceTarget();
            AI_State = (float)ActionState.Notice;
            AI_Timer = 0;
        }
    }

    private void Notice()
    {
        if (NPC.HasValidTarget && Main.player[NPC.target].Distance(NPC.Center) < ForgetDistance)
        {
            AI_Timer++;
            
            // right - left accelerate
            if (NPC.direction == 1 && NPC.velocity.X < MaxSpeed)
            {
                float velocityToUse = float.Clamp(NPC.velocity.X, 0f, MaxSpeed);
                if (NPC.velocity.X < 0)
                {
                    NPC.velocity.X += float.Lerp(0.15f, 0.01f, velocityToUse / MaxSpeed) * NPC.direction;
                }
                NPC.velocity.X += float.Lerp(0.01f, 0.11f, velocityToUse / MaxSpeed) * NPC.direction;
            }
            else if (NPC.direction == -1 && NPC.velocity.X > -MaxSpeed)
            {
                float velocityToUse = float.Clamp(NPC.velocity.X, -MaxSpeed, 0);
                if (NPC.velocity.X > 0)
                {
                    NPC.velocity.X += float.Lerp(0.15f, 0.01f, -velocityToUse / MaxSpeed) * NPC.direction;
                }
                NPC.velocity.X += float.Lerp(0.01f, 0.11f, -velocityToUse / MaxSpeed) * NPC.direction;
            }
            
            int x = (int)(NPC.position.X + NPC.width / 2 + ((NPC.width / 2 + float.Abs(NPC.velocity.X * 5) + 3) * NPC.direction));
            int y = (int)(NPC.position.Y + NPC.height - 8);
            Tile t_y0;
            Tile t_y1;
            // checks for presence of a tile in front of a bottom corner of where the NPC is facing
            // check if the wall is bigger than 1 tile
            t_y0 = Framing.GetTileSafely(new Vector2(x, y));
            t_y1 = Framing.GetTileSafely(new Vector2(x, y - 16));
            
            bool checkSlope = t_y0.Slope == 0 || (t_y0.Slope == SlopeType.SlopeDownLeft && NPC.direction == 1) || (t_y0.Slope == SlopeType.SlopeDownRight && NPC.direction == -1);
            bool tileSolidityCheck = t_y0.HasTile && Main.tileSolid[t_y0.TileType] && checkSlope;
            bool tileSolidityCheck1 = t_y1.HasTile && Main.tileSolid[t_y1.TileType];
            
            // Dust dust = Terraria.Dust.NewDustPerfect(new Vector2(x, y), 35, new Vector2(0f, 0f), 0, new Color(255,255,255), 1f);
            // dust.noGravity = true;
            // Dust dust2 = Terraria.Dust.NewDustPerfect(new Vector2(x, y - 16), 35, new Vector2(0f, 0f), 0, new Color(0,0,255), 1f);
            // dust2.noGravity = true;
            
            if (tileSolidityCheck)
            {
                float xVelToUse = GeneralUtils.ClampNpcVelocity(NPC.direction, NPC.velocity.X, 1f,MaxSpeed);
                if (tileSolidityCheck1)
                {
                    NPC.velocity = new Vector2(xVelToUse, -7.5f);
                    AI_State = (float)ActionState.Jump;
                    AI_Timer = 0;
                }
                else
                {
                    NPC.velocity = new Vector2(xVelToUse, -4.5f);
                    AI_State = (float)ActionState.Jump;
                    AI_Timer = 0;
                }
            }

            if (AI_Timer % 10 == 0)
            {
                NPC.TargetClosest();
            }
            
            if (AI_Timer == 60)
            {
                AI_Timer = 0;
            }
            
            if (NPC.velocity.Y > 0)
            {
                AI_State = (float)ActionState.Fall;
                AI_Timer = 0;
            }
        }
        else
        {
            NPC.TargetClosest(false);

            if (!NPC.HasValidTarget || Main.player[NPC.target].Distance(NPC.Center) > ForgetDistance)
            {
                AI_State = (float)ActionState.Asleep;
                AI_Timer = 0;
            }
        }
    }

    // This state is not responsible for jump itself but what happens after it
    private void Jump()
    {
        AI_Timer += 1;

        if (NPC.velocity.X == 0 && Math.Abs(NPC.velocity.Y) < 2f)
        {
            NPC.velocity.X += NPC.direction * 0.05f;
        }
        
        if (NPC.velocity.Y > 0)
        {
            if (NPC.velocity.X is > MaxSpeed*0.7f or < MaxSpeed*0.7f)
            {
                NPC.velocity.X = MaxSpeed * 0.7f * NPC.direction;
            }
            AI_State = (float)ActionState.Fall;
            AI_Timer = 0;
        }
    }

    private void Fall()
    {
        if (NPC.velocity.Y == 0)
        {
            NPC.velocity.X = float.Clamp(NPC.velocity.X, -MaxSpeed, MaxSpeed);
            AI_State = (float)ActionState.Asleep;
            AI_Timer = 0;
        }
    }
    
    private void KnockedBack()
    {
        if (NPC.velocity.Y == 0)
        {
            NPC.velocity.X = GeneralUtils.ClampNpcVelocity(NPC.direction, NPC.velocity.X, 1,MaxSpeed);
            AI_State = (float)ActionState.Asleep;
        }
    }

    public override bool ModifyCollisionData(Rectangle victimHitbox, ref int immunityCooldownSlot,
        ref MultipliableFloat damageMultiplier, ref Rectangle npcHitbox)
    {
        if (AI_State == (float)ActionState.Fall)
        {
            Rectangle extraDamageHitbox = new Rectangle(npcHitbox.X, npcHitbox.Y + 26, npcHitbox.Width - 10,
                npcHitbox.Height - 18);
            
            if (victimHitbox.Intersects(extraDamageHitbox))
            {
                damageMultiplier *= 1.5f;
            }
        }

        return true;
    }

    public override bool? CanFallThroughPlatforms()
    {
        if (AI_State == (float)ActionState.Fall && NPC.HasValidTarget && Main.player[NPC.target].Top.Y > NPC.Bottom.Y)
        {
            // If is currently falling, we want it to keep falling through platforms as long as it's above the player
            return true;
        }

        return false;
    }
}