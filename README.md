# NumeradorPdfApp

Aplicación WinForms en .NET para añadir numeración visible a todas las páginas de PDFs.

## Ejecutar

Compilar desde el proyecto:

```powershell
dotnet build NumeradorPdfApp\NumeradorPdfApp.csproj
```

Publicar ejecutable:

```powershell
dotnet publish NumeradorPdfApp\NumeradorPdfApp.csproj -c Release -r win-x64 --self-contained false -o publish
```

## Uso

1. Añade PDFs con `Añadir PDFs`, añade una carpeta con `Añadir carpeta`, o arrastra PDFs/carpetas a la ventana.
2. Indica el texto opcional antes del número. En blanco genera `1`, `2`, `3`; con `Página` genera `Página 1`, `Página 2`, `Página 3`.
3. Indica la ruta destino.
4. Pulsa `Numerar PDFs` para generar copias en destino conservando el mismo nombre de fichero.

Los PDFs se ordenan por nombre ascendente y se numeran de forma correlativa en todo el lote, sin reiniciar el contador entre documentos.
