using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;

namespace LaavensStuff.Utils;

public static class GeneralUtils
{
    public static void DrawBorderedRect(SpriteBatch spriteBatch, Color color, Color borderColor, Vector2 position, Vector2 size, int borderWidth) {
        var magicPixel = TextureAssets.MagicPixel.Value;

        spriteBatch.Draw(magicPixel, new Rectangle((int)position.X, (int)position.Y, (int)size.X, (int)size.Y), color);
        spriteBatch.Draw(magicPixel, new Rectangle((int)position.X - borderWidth, (int)position.Y - borderWidth, (int)size.X + borderWidth * 2, borderWidth), borderColor);
        spriteBatch.Draw(magicPixel, new Rectangle((int)position.X - borderWidth, (int)position.Y + (int)size.Y, (int)size.X + borderWidth * 2, borderWidth), borderColor);
        spriteBatch.Draw(magicPixel, new Rectangle((int)position.X - borderWidth, (int)position.Y, (int)borderWidth, (int)size.Y), borderColor);
        spriteBatch.Draw(magicPixel, new Rectangle((int)position.X + (int)size.X, (int)position.Y, (int)borderWidth, (int)size.Y), borderColor);
    }

    public static float ClampNpcVelocity(int direction, float velocity, float minVelocity, float maxVelocity)
    {
        if (direction == 1)
        {
            return float.Clamp(velocity, minVelocity, maxVelocity);
        } 
        return float.Clamp(velocity, -maxVelocity, -minVelocity);
    }
}