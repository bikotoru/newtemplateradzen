# 🛡️ Sistema de Validación de Formularios

## 📖 Índice

1. [Introducción](#introducción)
2. [Arquitectura del Sistema](#arquitectura-del-sistema)
3. [Guía de Uso Rápido](#guía-de-uso-rápido)
4. [Componentes Principales](#componentes-principales)
5. [Reglas de Validación](#reglas-de-validación)
6. [Ejemplos Completos](#ejemplos-completos)
7. [Personalización y Extensión](#personalización-y-extensión)
8. [Mejores Prácticas](#mejores-prácticas)

---

## 🎯 Introducción

El Sistema de Validación de Formularios proporciona una forma elegante, reactiva y performante de validar formularios en aplicaciones Blazor. Está diseñado para ser:

- ✅ **Simple de usar**: API fluida e intuitiva
- ✅ **Reactivo**: Validación automática sin intervención manual
- ✅ **Performante**: Validación optimizada solo cuando es necesario
- ✅ **Extensible**: Fácil agregar nuevas reglas de validación
- ✅ **Visual**: Feedback inmediato con estilos automáticos

---

## 🏗️ Arquitectura del Sistema

### Componentes Principales

```
FormValidator (Container)
    ↓ Provee ValidationContext
ValidatedInput (Wrapper)
    ↓ Envuelve cualquier input
RadzenFormField + Input
    ↓ Input real (RadzenTextBox, etc.)
```

### Flujo de Validación

```
1. Usuario sale del campo (blur)
   ↓
2. ValidatedInput captura el evento
   ↓
3. ValidationContext ejecuta reglas
   ↓
4. Resultado se propaga reactivamente
   ↓
5. UI se actualiza automáticamente
```

---

## ⚡ Guía de Uso Rápido

### Paso 1: Definir las Reglas

```csharp
private FormValidationRules GetValidationRules()
{
    return FormValidationRulesBuilder
        .Create()
        .Field("Nombre", field => field
            .Required("El nombre es obligatorio")
            .Length(3, 100, "Entre 3 y 100 caracteres"))
        .Field("Email", field => field
            .Required("El email es obligatorio")
            .Email("Formato de email inválido"))
        .Build();
}
```

### Paso 2: Estructurar el Formulario

```razor
<FormValidator Entity="entity" Rules="@GetValidationRules()">

    <ValidatedInput FieldName="Nombre" Value="@entity.Nombre">
        <RadzenFormField Text="Nombre">
            <RadzenTextBox @oninput="@(v => entity.Nombre = v.Value?.ToString())" 
                           Value="@entity.Nombre"
                           Placeholder="Ingrese su nombre" />
        </RadzenFormField>
    </ValidatedInput>

    <ValidatedInput FieldName="Email" Value="@entity.Email">
        <RadzenFormField Text="Email">
            <RadzenTextBox @oninput="@(v => entity.Email = v.Value?.ToString())" 
                           Value="@entity.Email"
                           Placeholder="ejemplo@correo.com" />
        </RadzenFormField>
    </ValidatedInput>

</FormValidator>
```

### Paso 3: Validación al Guardar

```csharp
private async Task SaveForm()
{
    // Validación manual (opcional - ya se valida automáticamente)
    if (string.IsNullOrWhiteSpace(entity.Nombre))
    {
        errorMessage = "Complete los campos requeridos";
        return;
    }
    
    // Continuar con el guardado...
}
```

---

## 🧩 Componentes Principales

### FormValidator

**Propósito**: Container principal que proporciona el contexto de validación.

```razor
<FormValidator Entity="@miEntidad" Rules="@GetValidationRules()">
    <!-- Aquí van los ValidatedInput -->
</FormValidator>
```

**Parámetros**:
- `Entity`: La entidad que se está validando
- `Rules`: Las reglas de validación definidas con el builder

### ValidatedInput

**Propósito**: Wrapper que envuelve cualquier input y proporciona validación automática.

```razor
<ValidatedInput FieldName="NombreDelCampo" Value="@valorActual">
    <RadzenFormField Text="Etiqueta">
        <RadzenTextBox @oninput="@(v => entity.Campo = v.Value?.ToString())" 
                       Value="@entity.Campo" />
    </RadzenFormField>
</ValidatedInput>
```

**Parámetros**:
- `FieldName`: Nombre del campo que debe coincidir con el definido en las reglas
- `Value`: Valor actual del campo para validación
- `ChildContent`: El contenido (RadzenFormField + Input)

---

## 📏 Reglas de Validación

### Reglas Disponibles

#### Required
```csharp
.Required("Mensaje de error personalizado")
```

#### Length
```csharp
.Length(min: 3, max: 100, "Entre 3 y 100 caracteres")
.MinLength(5, "Mínimo 5 caracteres")
.MaxLength(255, "Máximo 255 caracteres")
```

#### Email
```csharp
.Email("Formato de email inválido")
```

#### Personalizada
```csharp
.Custom(value => {
    // Tu lógica de validación
    if (/* condición */) 
        return ValidationResult.Success();
    else 
        return ValidationResult.Error("Mensaje de error");
})
```

### Ejemplos de Reglas Complejas

```csharp
private FormValidationRules GetValidationRules()
{
    return FormValidationRulesBuilder
        .Create()
        
        // Campo básico requerido
        .Field("Nombre", field => field
            .Required("El nombre es obligatorio")
            .Length(2, 50, "Entre 2 y 50 caracteres"))
        
        // Email con validación
        .Field("Email", field => field
            .Required("El email es obligatorio")
            .Email("Debe ser un email válido"))
        
        // Campo opcional con límite
        .Field("Comentarios", field => field
            .MaxLength(500, "Máximo 500 caracteres"))
        
        // Validación personalizada
        .Field("Edad", field => field
            .Required("La edad es obligatoria")
            .Custom(value => {
                if (int.TryParse(value?.ToString(), out int edad))
                {
                    if (edad >= 18 && edad <= 120)
                        return ValidationResult.Success();
                    else
                        return ValidationResult.Error("Debe ser entre 18 y 120 años");
                }
                return ValidationResult.Error("Debe ser un número válido");
            }))
        
        .Build();
}
```

---

## 💡 Ejemplos Completos

### Ejemplo 1: Formulario de Usuario

```razor
@page "/usuario/crear"

<PageTitle>Crear Usuario</PageTitle>

<RadzenCard class="p-4">
    <h3>Nuevo Usuario</h3>
    
    <FormValidator Entity="usuario" Rules="@GetValidationRules()">
        
        <RadzenStack Gap="1rem">
            
            <!-- Nombre -->
            <ValidatedInput FieldName="Nombre" Value="@usuario.Nombre">
                <RadzenFormField Text="Nombre Completo">
                    <RadzenTextBox @oninput="@(v => usuario.Nombre = v.Value?.ToString())" 
                                   Value="@usuario.Nombre"
                                   Placeholder="Ingrese nombre completo" />
                </RadzenFormField>
            </ValidatedInput>
            
            <!-- Email -->
            <ValidatedInput FieldName="Email" Value="@usuario.Email">
                <RadzenFormField Text="Correo Electrónico">
                    <RadzenTextBox @oninput="@(v => usuario.Email = v.Value?.ToString())" 
                                   Value="@usuario.Email"
                                   Placeholder="ejemplo@correo.com" />
                </RadzenFormField>
            </ValidatedInput>
            
            <!-- Teléfono -->
            <ValidatedInput FieldName="Telefono" Value="@usuario.Telefono">
                <RadzenFormField Text="Teléfono">
                    <RadzenTextBox @oninput="@(v => usuario.Telefono = v.Value?.ToString())" 
                                   Value="@usuario.Telefono"
                                   Placeholder="(opcional)" />
                </RadzenFormField>
            </ValidatedInput>
            
            <!-- Contraseña -->
            <ValidatedInput FieldName="Password" Value="@usuario.Password">
                <RadzenFormField Text="Contraseña">
                    <RadzenPassword @oninput="@(v => usuario.Password = v.Value?.ToString())" 
                                   Value="@usuario.Password"
                                   Placeholder="Mínimo 8 caracteres" />
                </RadzenFormField>
            </ValidatedInput>
            
            <!-- Confirmar Contraseña -->
            <ValidatedInput FieldName="ConfirmPassword" Value="@confirmPassword">
                <RadzenFormField Text="Confirmar Contraseña">
                    <RadzenPassword @oninput="@(v => confirmPassword = v.Value?.ToString())" 
                                   Value="@confirmPassword"
                                   Placeholder="Repita la contraseña" />
                </RadzenFormField>
            </ValidatedInput>
            
        </RadzenStack>
        
    </FormValidator>
    
    <div class="mt-3">
        <RadzenButton Text="Crear Usuario" 
                      Icon="person_add" 
                      ButtonStyle="ButtonStyle.Primary"
                      Click="@SaveUser" />
        <RadzenButton Text="Cancelar" 
                      Icon="cancel" 
                      ButtonStyle="ButtonStyle.Light"
                      Click="@Cancel" />
    </div>
    
</RadzenCard>

@code {
    private Usuario usuario = new();
    private string confirmPassword = "";
    
    private FormValidationRules GetValidationRules()
    {
        return FormValidationRulesBuilder
            .Create()
            .Field("Nombre", field => field
                .Required("El nombre es obligatorio")
                .Length(2, 100, "Entre 2 y 100 caracteres"))
                
            .Field("Email", field => field
                .Required("El email es obligatorio")
                .Email("Debe ser un email válido"))
                
            .Field("Telefono", field => field
                .Length(10, 15, "Entre 10 y 15 dígitos"))
                
            .Field("Password", field => field
                .Required("La contraseña es obligatoria")
                .MinLength(8, "Mínimo 8 caracteres")
                .Custom(value => ValidatePassword(value?.ToString())))
                
            .Field("ConfirmPassword", field => field
                .Required("Confirme la contraseña")
                .Custom(value => {
                    if (value?.ToString() == usuario.Password)
                        return ValidationResult.Success();
                    return ValidationResult.Error("Las contraseñas no coinciden");
                }))
                
            .Build();
    }
    
    private ValidationResult ValidatePassword(string? password)
    {
        if (string.IsNullOrEmpty(password)) 
            return ValidationResult.Error("Contraseña requerida");
            
        var hasUpper = password.Any(char.IsUpper);
        var hasLower = password.Any(char.IsLower);
        var hasDigit = password.Any(char.IsDigit);
        
        if (!hasUpper || !hasLower || !hasDigit)
            return ValidationResult.Error("Debe contener mayúsculas, minúsculas y números");
            
        return ValidationResult.Success();
    }
    
    private async Task SaveUser()
    {
        // Validación final antes de guardar
        if (!IsFormValid()) return;
        
        // Lógica de guardado...
    }
    
    private bool IsFormValid()
    {
        if (string.IsNullOrWhiteSpace(usuario.Nombre) || 
            string.IsNullOrWhiteSpace(usuario.Email) ||
            string.IsNullOrWhiteSpace(usuario.Password) ||
            usuario.Password != confirmPassword)
        {
            // Mostrar mensaje de error
            return false;
        }
        return true;
    }
    
    private void Cancel()
    {
        // Navegar de vuelta
    }
}
```

### Ejemplo 2: Formulario de Producto

```razor
<FormValidator Entity="producto" Rules="@GetProductValidationRules()">
    
    <RadzenTabs>
        <RadzenTabsItem Text="Información Básica">
            
            <ValidatedInput FieldName="Codigo" Value="@producto.Codigo">
                <RadzenFormField Text="Código del Producto">
                    <RadzenTextBox @oninput="@(v => producto.Codigo = v.Value?.ToString())" 
                                   Value="@producto.Codigo"
                                   Placeholder="SKU-001" />
                </RadzenFormField>
            </ValidatedInput>
            
            <ValidatedInput FieldName="Nombre" Value="@producto.Nombre">
                <RadzenFormField Text="Nombre del Producto">
                    <RadzenTextBox @oninput="@(v => producto.Nombre = v.Value?.ToString())" 
                                   Value="@producto.Nombre"
                                   Placeholder="Nombre del producto" />
                </RadzenFormField>
            </ValidatedInput>
            
            <ValidatedInput FieldName="Precio" Value="@producto.Precio">
                <RadzenFormField Text="Precio">
                    <RadzenNumeric @oninput="@(v => producto.Precio = decimal.Parse(v.Value?.ToString() ?? "0"))" 
                                   Value="@producto.Precio"
                                   Format="c" />
                </RadzenFormField>
            </ValidatedInput>
            
        </RadzenTabsItem>
        
        <RadzenTabsItem Text="Detalles">
        
            <ValidatedInput FieldName="Descripcion" Value="@producto.Descripcion">
                <RadzenFormField Text="Descripción">
                    <RadzenTextArea @oninput="@(v => producto.Descripcion = v.Value?.ToString())" 
                                    Value="@producto.Descripcion"
                                    Rows="4" />
                </RadzenFormField>
            </ValidatedInput>
            
            <ValidatedInput FieldName="CategoriaId" Value="@producto.CategoriaId">
                <RadzenFormField Text="Categoría">
                    <RadzenDropDown Data="@categorias"
                                    @bind-Value="producto.CategoriaId"
                                    TextProperty="Nombre"
                                    ValueProperty="Id"
                                    Placeholder="Seleccione una categoría" />
                </RadzenFormField>
            </ValidatedInput>
            
        </RadzenTabsItem>
    </RadzenTabs>
    
</FormValidator>

@code {
    private FormValidationRules GetProductValidationRules()
    {
        return FormValidationRulesBuilder
            .Create()
            .Field("Codigo", field => field
                .Required("El código es obligatorio")
                .Length(3, 20, "Entre 3 y 20 caracteres")
                .Custom(async value => await ValidateUniqueCode(value?.ToString())))
                
            .Field("Nombre", field => field
                .Required("El nombre es obligatorio")
                .Length(5, 200, "Entre 5 y 200 caracteres"))
                
            .Field("Precio", field => field
                .Required("El precio es obligatorio")
                .Custom(value => {
                    if (decimal.TryParse(value?.ToString(), out decimal precio) && precio > 0)
                        return ValidationResult.Success();
                    return ValidationResult.Error("Debe ser un precio válido mayor a 0");
                }))
                
            .Field("Descripcion", field => field
                .MaxLength(1000, "Máximo 1000 caracteres"))
                
            .Field("CategoriaId", field => field
                .Required("Seleccione una categoría"))
                
            .Build();
    }
    
    private async Task<ValidationResult> ValidateUniqueCode(string? codigo)
    {
        if (string.IsNullOrEmpty(codigo)) return ValidationResult.Success();
        
        // Simulación de validación asíncrona
        await Task.Delay(100);
        var exists = await ProductService.ExistsCodeAsync(codigo);
        
        return exists 
            ? ValidationResult.Error("Este código ya existe") 
            : ValidationResult.Success();
    }
}
```

---

## 🎨 Personalización y Extensión

### Crear Reglas Personalizadas

```csharp
public class PhoneRule : IValidationRule
{
    private readonly string _errorMessage;
    
    public PhoneRule(string errorMessage = "Número de teléfono inválido")
    {
        _errorMessage = errorMessage;
    }
    
    public Task<ValidationResult> ValidateAsync(object? value)
    {
        if (value is not string phone || string.IsNullOrEmpty(phone))
            return Task.FromResult(ValidationResult.Success());
            
        // Validar formato de teléfono
        var phoneRegex = @"^\+?[1-9]\d{1,14}$";
        var isValid = System.Text.RegularExpressions.Regex.IsMatch(phone, phoneRegex);
        
        return Task.FromResult(isValid 
            ? ValidationResult.Success() 
            : ValidationResult.Error(_errorMessage));
    }
}

// Extensión para usar en el builder
public static class ValidationExtensions
{
    public static FieldValidationBuilder Phone(this FieldValidationBuilder builder, string? errorMessage = null)
    {
        // Agregar la regla personalizada al builder
        return builder; // Implementar lógica
    }
}
```

### Estilos CSS Personalizados

```css
/* Personalizar colores de validación */
.validated-input.has-error .rz-textbox {
    border-color: #e74c3c !important;
    box-shadow: 0 0 0 0.2rem rgba(231, 76, 60, 0.25) !important;
}

.validated-input.is-valid .rz-textbox {
    border-color: #27ae60 !important;
    box-shadow: 0 0 0 0.1rem rgba(39, 174, 96, 0.25) !important;
}

/* Personalizar mensajes de error */
.validation-error {
    background: #ffe6e6;
    border: 1px solid #ffcccc;
    border-radius: 4px;
    padding: 0.5rem;
    margin-top: 0.25rem;
    color: #c62828;
}
```

---

## 🎯 Mejores Prácticas

### 1. Estructura de Reglas
```csharp
// ✅ Bueno: Reglas claras y específicas
.Field("Email", field => field
    .Required("El email es obligatorio")
    .Email("Debe ser un formato válido")
    .MaxLength(255, "Máximo 255 caracteres"))

// ❌ Evitar: Mensajes genéricos
.Field("Email", field => field
    .Required("Campo requerido")
    .Email("Inválido"))
```

### 2. Validación Asíncrona
```csharp
// ✅ Bueno: Validación asíncrona para operaciones costosas
.Field("Username", field => field
    .Required("Usuario requerido")
    .Custom(async value => {
        var exists = await UserService.ExistsAsync(value?.ToString());
        return exists 
            ? ValidationResult.Error("Usuario ya existe") 
            : ValidationResult.Success();
    }))
```

### 3. Reutilización de Reglas
```csharp
// ✅ Bueno: Crear métodos reutilizables
public static class CommonValidations
{
    public static FieldValidationBuilder StandardName(this FieldValidationBuilder builder)
    {
        return builder
            .Required("El nombre es obligatorio")
            .Length(2, 100, "Entre 2 y 100 caracteres")
            .Matches(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$", "Solo letras y espacios");
    }
    
    public static FieldValidationBuilder StandardEmail(this FieldValidationBuilder builder)
    {
        return builder
            .Required("El email es obligatorio")
            .Email("Formato de email inválido")
            .MaxLength(255, "Máximo 255 caracteres");
    }
}

// Uso
.Field("Nombre", field => field.StandardName())
.Field("Email", field => field.StandardEmail())
```

### 4. Validación Condicional
```csharp
.Field("Telefono", field => field
    .Custom(value => {
        // Solo validar si se proporcionó un valor
        if (string.IsNullOrEmpty(value?.ToString()))
            return ValidationResult.Success();
            
        // Validar formato si hay valor
        return ValidatePhoneFormat(value.ToString());
    }))
```

### 5. Mensajes Localizados
```csharp
// ✅ Usar recursos para localización
.Field("Nombre", field => field
    .Required(Localizer["NameRequired"])
    .Length(2, 100, Localizer["NameLength"]))
```

---

## ⚠️ Limitaciones y Consideraciones

### Rendimiento
- Las validaciones asíncronas pueden impactar el rendimiento
- Usa debouncing para validaciones costosas
- Evita validaciones complejas en campos de escritura frecuente

### Compatibilidad
- Funciona con todos los componentes de Radzen
- Compatible con inputs HTML estándar
- Requiere .NET 8+ y Blazor Server/WASM

### Limitaciones Actuales
- No soporta validación cross-form
- Validación grupal limitada
- Sin soporte para validación condicional compleja entre campos

---

## 🚀 Próximas Mejoras

- [ ] Validación cross-field mejorada
- [ ] Soporte para grupos de validación
- [ ] Validación condicional avanzada
- [ ] Integración con DataAnnotations
- [ ] Más reglas predefinidas
- [ ] Mejor soporte para localización

---

## 📞 Soporte

Para dudas o problemas con el sistema de validación:

1. Consulta los ejemplos en esta documentación
2. Revisa el código fuente en `/Frontend/Components/Validation/`
3. Contacta al equipo de desarrollo

---

*Documentación actualizada: Enero 2025*