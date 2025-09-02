import os

def exportar_archivos(carpeta_origen, archivo_salida):
    # Lista de carpetas a omitir
    #carpetas_omitidas = {'bin', 'obj', 'Server', 'NewConexion'}
    carpetas_omitidas = {'bin', 'obj', 'NewConexion', 'Modules', 'Form', 'Demos', 'Admin'}

    with open(archivo_salida, 'w', encoding='utf-8') as salida:
        # Recorre la carpeta de manera recursiva
        for carpeta_raiz, carpetas, archivos in os.walk(carpeta_origen):
            # Filtra las carpetas omitidas
            carpetas[:] = [c for c in carpetas if c not in carpetas_omitidas]

            for archivo in archivos:
                # Construye la ruta completa del archivo
                ruta_completa = os.path.join(carpeta_raiz, archivo)

                # Filtra por extensiones .cs y .razor
                if archivo.endswith(('.cs', '.razor', '.js', '.html')):
                    with open(ruta_completa, 'r', encoding='utf-8') as entrada:
                        contenido = entrada.read()
                    
                    # Escribe el contenido en el archivo de salida con el formato deseado
                    salida.write(f"// Ruta: {ruta_completa}\n")
                    salida.write(contenido)
                    salida.write("\n\n")
                    print(f"Agregado: {ruta_completa}")

# Especifica la carpeta donde están los archivos y el nombre del archivo combinado
carpeta_actual = os.getcwd()  # Cambia esta ruta si necesitas otra carpeta
archivo_combinado = os.path.join(carpeta_actual, 'exportado.txt')

exportar_archivos(carpeta_actual, archivo_combinado)

print(f"Todos los archivos se exportaron en {archivo_combinado}")
