# Fase 1 - Sistema de Ventas (5 entidades)

## 📋 Comandos (copiar y pegar uno por uno):

```bash
# 1. Marca (Entidad normal - completa)
python3 tools/forms/entity-generator.py --entity "Marca" --plural "Marcas" --module "Catalogo" --target todo --fields "nombre:string:255" "codigointerno:string:50" --form-fields "nombre:required:placeholder=Nombre de la marca:min_length=2" "codigointerno:required:unique:placeholder=Código interno:max_length=50" --grid-fields "nombre:200px:left:sf" "codigointerno:120px:left:sf" --search-fields "nombre,codigointerno"

# 2. Categoria (Entidad normal - completa)
python3 tools/forms/entity-generator.py --entity "Categoria" --plural "Categorias" --module "Catalogo" --target todo --fields "nombre:string:255" "codigointerno:string:50" --form-fields "nombre:required:placeholder=Nombre de la categoría:min_length=2" "codigointerno:required:unique:placeholder=Código interno:max_length=50" --grid-fields "nombre:200px:left:sf" "codigointerno:120px:left:sf" --search-fields "nombre,codigointerno"

# 3. Producto (Entidad con relaciones - completa)
python3 tools/forms/entity-generator.py --entity "Producto" --plural "Productos" --module "Catalogo" --target todo --fields "nombre:string:255" "codigosku:string:100" "precioventa:int" "preciocompra:int" --fk "marca_id:marca" "categoria_id:categoria" --form-fields "nombre:required:placeholder=Nombre del producto:min_length=2" "codigosku:required:unique:placeholder=Código SKU:max_length=100" "precioventa:required:min=0:placeholder=Precio de venta" "preciocompra:required:min=0:placeholder=Precio de compra" --grid-fields "nombre:200px:left:sf" "codigosku:120px:left:sf" "precioventa:120px:right:sf" "preciocompra:120px:right:sf" "marca_id->Marca.Nombre:150px:left:f" "categoria_id->Categoria.Nombre:150px:left:f" --lookups "marca_id:marca:Nombre:required:cache:form,grid" "categoria_id:categoria:Nombre:required:cache:form,grid" --search-fields "nombre,codigosku"

# 4. Venta (Entidad normal - completa)
python3 tools/forms/entity-generator.py --entity "Venta" --plural "Ventas" --module "Ventas" --target todo --fields "numventa:int" "montototal:int" --form-fields "numventa:required:unique:placeholder=Número de venta" "montototal:required:min=0:placeholder=Monto total" --grid-fields "numventa:120px:left:sf" "montototal:150px:right:sf" --search-fields "numventa"

# 5. Venta ↔ Productos (Relación N:N - SINTAXIS ELEGANTE) ✅ 
python3 tools/forms/entity-generator.py --source venta --to productos --module "Ventas" --target db --fields "cantidad:int" "precioneto:int" "descuentopeso:int" "descuentoporcentaje:decimal:5,2" "montototal:int" --fk "venta_id:venta" "producto_id:producto"
```

## ℹ️ Info:
- **Ejecutar uno por uno en orden**
- **Entidades normales** (`--target todo`): crea tabla BD + backend + frontend completo
- **Tablas NN** (`--target db`): crea SOLO tabla en base de datos (sin interfaz)
- URLs generadas: `/catalogo/marca/list`, `/catalogo/categoria/list`, `/catalogo/producto/list`, `/ventas/venta/list`

## 📝 Notas específicas:
- **NUEVA SINTAXIS ELEGANTE**: `--source venta --to productos` genera automáticamente `nn_venta_productos`
- **Organización automática**: Modelo se crea en `Shared.Models/Entities/NN/NnVentaProductos.cs`
- **Namespace**: `Shared.Models.Entities.NN` (organizado automáticamente por el sync)
- **Permisos especiales**: `VENTA.ADDTARGET`, `VENTA.DELETETARGET`, `VENTA.EDITTARGET`
- No tiene interfaz de usuario propia (solo tabla BD)
- Se gestiona a través de las entidades Venta y Producto relacionadas
- Los precios están como `int` según especificación (considerar cambiar a `decimal` si necesitas decimales)