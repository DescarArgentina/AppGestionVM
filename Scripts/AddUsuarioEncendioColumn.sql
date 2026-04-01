-- Script para agregar la columna UsuarioEncendio a la tabla MaquinasVirtuales
-- Ejecutar este script en la base de datos ControlVM con permisos de administrador

IF NOT EXISTS (
    SELECT 1 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE [TABLE_NAME] = 'MaquinasVirtuales' 
    AND [COLUMN_NAME] = 'UsuarioEncendio'
)
BEGIN
    ALTER TABLE [dbo].[MaquinasVirtuales]
    ADD [UsuarioEncendio] NVARCHAR(255) NULL;

    PRINT 'Columna UsuarioEncendio agregada correctamente.';
END
ELSE
BEGIN
    PRINT 'La columna UsuarioEncendio ya existe.';
END
