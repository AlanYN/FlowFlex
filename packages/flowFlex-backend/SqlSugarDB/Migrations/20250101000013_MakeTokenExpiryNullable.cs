using SqlSugar;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Make token_expiry field nullable in ff_user_invitations table
    /// </summary>
    public class MakeTokenExpiryNullable_20250101000013
    {
        public static void Up(ISqlSugarClient db)
        {
            // Make token_expiry column nullable
            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_user_invitations 
                ALTER COLUMN token_expiry DROP NOT NULL;
            ");
        }

        public static void Down(ISqlSugarClient db)
        {
            // Revert token_expiry column to NOT NULL
            // Note: This requires setting a default value for existing NULL rows
            db.Ado.ExecuteCommand(@"
                UPDATE ff_user_invitations 
                SET token_expiry = NOW() + INTERVAL '1 year' 
                WHERE token_expiry IS NULL;
                
                ALTER TABLE ff_user_invitations 
                ALTER COLUMN token_expiry SET NOT NULL;
            ");
        }
    }
}