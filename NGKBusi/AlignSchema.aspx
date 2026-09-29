<%@ Page Language="C#" ValidateRequest="false" %>
<%@ Import Namespace="System" %>
<%@ Import Namespace="System.Linq" %>
<%@ Import Namespace="System.Data" %>
<%@ Import Namespace="System.Data.Entity" %>
<%@ Import Namespace="System.Data.Entity.Infrastructure" %>
<%@ Import Namespace="System.Data.SqlClient" %>
<%@ Import Namespace="System.Collections.Generic" %>
<%@ Import Namespace="System.Data.Entity.Core.Metadata.Edm" %>

<script runat="server">
    protected void Page_Load(object sender, EventArgs e)
    {
        Response.ContentType = "text/plain";
        Response.Write("Starting Schema Alignment for ALL Models...\n\n");
        
        string connStr = "Data Source=192.168.1.248;Initial Catalog=NGKBusi_Dev;User ID=sa;Password=secofr;MultipleActiveResultSets=True;";
        
        try 
        {
            var existingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var existingTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            // 1. Get current DB schema
            using (var conn = new SqlConnection(connStr)) {
                conn.Open();
                using (var cmd = new SqlCommand("SELECT TABLE_SCHEMA, TABLE_NAME, COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS", conn)) {
                    using (var reader = cmd.ExecuteReader()) {
                        while(reader.Read()) {
                            string schema = reader["TABLE_SCHEMA"].ToString();
                            string table = reader["TABLE_NAME"].ToString();
                            string col = reader["COLUMN_NAME"].ToString();
                            existingTables.Add($"{schema}.{table}");
                            existingColumns.Add($"{schema}.{table}.{col}");
                        }
                    }
                }
            }
            
            // 2. Iterate DbContexts
            var assembly = typeof(NGKBusi.MvcApplication).Assembly;
            var dbContextTypes = assembly.GetTypes().Where(t => typeof(DbContext).IsAssignableFrom(t) && !t.IsAbstract).ToList();
            
            int newColumnsAdded = 0;
            int errorCount = 0;
            
            using (var conn = new SqlConnection(connStr)) {
                conn.Open();
                
                foreach(var t in dbContextTypes) {
                    try {
                        var initMethod = typeof(Database).GetMethod("SetInitializer").MakeGenericMethod(t);
                        initMethod.Invoke(null, new object[] { null });
                        
                        DbContext ctx = null;
                        try {
                            ctx = (DbContext)Activator.CreateInstance(t);
                        } catch { continue; }
                        
                        if (ctx == null) continue;
                        
                        var objCtx = ((IObjectContextAdapter)ctx).ObjectContext;
                        var tables = objCtx.MetadataWorkspace.GetItems<EntityType>(DataSpace.SSpace);
                        
                        foreach (var table in tables) {
                            string schema = table.MetadataProperties.Contains("Schema") && table.MetadataProperties["Schema"].Value != null 
                                ? table.MetadataProperties["Schema"].Value.ToString() 
                                : "dbo";
                                
                            string tableName = table.MetadataProperties.Contains("Table") && table.MetadataProperties["Table"].Value != null 
                                ? table.MetadataProperties["Table"].Value.ToString() 
                                : table.Name;
                            
                            // If table doesn't exist, we skip creating the whole table because EF CreateDatabaseScript is better for that, 
                            // and we already ran that. We only care about missing columns in existing tables!
                            if (!existingTables.Contains($"{schema}.{tableName}")) {
                                continue; 
                            }
                            
                            foreach (var prop in table.Properties) {
                                string colName = prop.Name;
                                if (!existingColumns.Contains($"{schema}.{tableName}.{colName}")) {
                                    // Column is missing! Construct ALTER TABLE
                                    string sqlTypeName = prop.TypeUsage.EdmType.Name;
                                    string typeDeclaration = sqlTypeName;
                                    
                                    if (sqlTypeName == "varchar" || sqlTypeName == "nvarchar" || sqlTypeName == "char") {
                                        var facets = prop.TypeUsage.Facets;
                                        var maxLength = facets.Contains("MaxLength") && facets["MaxLength"].Value != null ? facets["MaxLength"].Value.ToString() : "MAX";
                                        typeDeclaration += $"({maxLength})";
                                    } else if (sqlTypeName == "decimal" || sqlTypeName == "numeric") {
                                        var facets = prop.TypeUsage.Facets;
                                        var precision = facets.Contains("Precision") && facets["Precision"].Value != null ? facets["Precision"].Value.ToString() : "18";
                                        var scale = facets.Contains("Scale") && facets["Scale"].Value != null ? facets["Scale"].Value.ToString() : "0";
                                        typeDeclaration += $"({precision}, {scale})";
                                    }
                                    
                                    string isNullable = prop.Nullable ? "NULL" : "NULL"; // Always add as NULL to prevent errors on existing rows
                                    
                                    string alterSql = $"ALTER TABLE [{schema}].[{tableName}] ADD [{colName}] {typeDeclaration} {isNullable};";
                                    
                                    try {
                                        using (var cmd = new SqlCommand(alterSql, conn)) {
                                            cmd.ExecuteNonQuery();
                                            newColumnsAdded++;
                                            Response.Write($"[ADDED] Column '{colName}' added to '{schema}.{tableName}'\n");
                                        }
                                        existingColumns.Add($"{schema}.{tableName}.{colName}");
                                    } catch (Exception ex) {
                                        Response.Write($"[ERROR] Failed to add '{colName}' to '{tableName}': {ex.Message}\n");
                                        errorCount++;
                                    }
                                }
                            }
                        }
                    } catch (Exception ex) {
                        Response.Write($"[ERROR processing {t.Name}] {ex.Message}\n");
                    }
                }
            }
            
            Response.Write($"\nAlignment completed! Added {newColumnsAdded} missing columns. Encountered {errorCount} errors.\n");
        }
        catch(Exception ex)
        {
            Response.Write("CRITICAL ERROR: " + ex.ToString());
        }
    }
</script>
