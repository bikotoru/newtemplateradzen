# Dynamic Form Dialog System Documentation

This documentation explains how to use the Custom Radzen Dialog Form System, a flexible component library for creating dynamic forms in Blazor applications.

## Table of Contents
- [Overview](#overview)
- [Core Components](#core-components)
- [Basic Usage](#basic-usage)
- [Form Field Types](#form-field-types)
- [Complete Examples](#complete-examples)
  - [Simple Text Input Dialog](#simple-text-input-dialog)
  - [Multi-Field Form Dialog](#multi-field-form-dialog)
  - [Form with Validation](#form-with-validation)
- [API Reference](#api-reference)

## Overview

The Dynamic Form Dialog System allows you to create various types of interactive dialogs with forms without having to create separate Blazor components for each use case. The system includes:

- Dynamic multi-field forms with various input types
- Simple single-field prompt dialogs
- Built-in validation
- Customizable styling and layout

## Core Components

The system consists of the following key components:

1. **DynamicFormDialog** - The main component for complex forms with multiple fields
2. **SimpleInputDialog** - A simplified dialog for single-input prompts
3. **FormDialogExtensions** - Extension methods for the Radzen DialogService
4. **SimpleDialogExtensions** - Extension methods for simple dialogs
5. **FormField** - Class representing a form field with various properties

## Basic Usage

### Adding the Required Services

First, make sure you have the DialogService registered in your application:

```csharp
// In Program.cs or Startup.cs
builder.Services.AddScoped<DialogService>();
```

### Injecting the Service

In your Blazor components:

```csharp
@inject DialogService DialogService
```

### Opening a Simple Dialog

To open a simple text input dialog:

```csharp
var result = await DialogService.PromptTextAsync(
    "User Input", 
    "Enter your name", 
    defaultValue: "John Doe");

if (result != null)
{
    // User entered a value
    string name = result;
}
```

### Opening a Dynamic Form

To open a multi-field form dialog:

```csharp
var options = new FormDialogOptions
{
    Fields = new List<FormField>
    {
        new FormField 
        { 
            Name = "name", 
            Label = "Full Name", 
            Type = FormFieldType.Text, 
            Required = true 
        },
        new FormField 
        { 
            Name = "age", 
            Label = "Age", 
            Type = FormFieldType.Numeric 
        }
    }
};

var result = await DialogService.OpenFormAsync("User Details", options);

if (result != null)
{
    // User submitted the form
    string name = result["name"]?.ToString();
    decimal age = Convert.ToDecimal(result["age"]);
}
```

## Form Field Types

The system supports the following form field types:

| Type | Description | Component Used |
|------|-------------|---------------|
| `Text` | Single-line text input | RadzenTextBox |
| `TextArea` | Multi-line text input | RadzenTextArea |
| `Numeric` | Number input | RadzenNumeric |
| `Date` | Date picker | RadzenDatePicker |
| `DateTime` | Date and time picker | RadzenDatePicker with ShowTime |
| `Select` | Dropdown selection | RadzenDropDown |
| `Checkbox` | Boolean checkbox | RadzenCheckBox |
| `Radio` | Radio button list | RadzenRadioButtonList |

## Complete Examples

### Simple Text Input Dialog

```csharp
@page "/example-simple-input"
@inject DialogService DialogService

<RadzenButton Text="Show Text Input" Click="@ShowTextInputDialog" />

@code {
    private async Task ShowTextInputDialog()
    {
        var result = await DialogService.PromptTextAsync(
            "Enter Text",
            "Your Name",
            defaultValue: "",
            required: true);
            
        if (result != null)
        {
            // Process the result
            Console.WriteLine($"User entered: {result}");
        }
    }
}
```

### Multi-Field Form Dialog

```csharp
@page "/example-multi-field"
@inject DialogService DialogService

<RadzenButton Text="Show Form" Click="@ShowFormDialog" />

@code {
    private async Task ShowFormDialog()
    {
        var options = new FormDialogOptions
        {
            Fields = new List<FormField>
            {
                new FormField 
                { 
                    Name = "name", 
                    Label = "Full Name", 
                    Type = FormFieldType.Text, 
                    Required = true,
                    Placeholder = "Enter your full name" 
                },
                new FormField 
                { 
                    Name = "email", 
                    Label = "Email Address", 
                    Type = FormFieldType.Text,
                    Required = true,
                    HelpText = "We'll never share your email" 
                },
                new FormField 
                { 
                    Name = "birthDate", 
                    Label = "Birth Date", 
                    Type = FormFieldType.Date 
                },
                new FormField 
                { 
                    Name = "subscribe", 
                    Label = "Subscribe to newsletter", 
                    Type = FormFieldType.Checkbox,
                    DefaultValue = true
                }
            },
            SaveButtonText = "Submit",
            CancelButtonText = "Close",
            Horizontal = true
        };

        var result = await DialogService.OpenFormAsync("User Registration", options);

        if (result != null)
        {
            // User submitted the form
            string name = result["name"]?.ToString();
            string email = result["email"]?.ToString();
            DateTime? birthDate = null;
            if (result["birthDate"] != null)
            {
                birthDate = Convert.ToDateTime(result["birthDate"]);
            }
            bool subscribe = Convert.ToBoolean(result["subscribe"]);
            
            // Process the data...
        }
    }
}
```

### Form with Validation

```csharp
@page "/example-validation"
@inject DialogService DialogService

<RadzenButton Text="Show Form with Validation" Click="@ShowValidatedForm" />

@code {
    private async Task ShowValidatedForm()
    {
        var options = new FormDialogOptions
        {
            Fields = new List<FormField>
            {
                new FormField 
                { 
                    Name = "email", 
                    Label = "Email Address", 
                    Type = FormFieldType.Text, 
                    Required = true,
                    RequiredMessage = "Please enter your email address",
                    Validator = (value) => {
                        var email = value?.ToString();
                        if (string.IsNullOrEmpty(email) || !email.Contains("@"))
                            return new ValidationResult("Please enter a valid email address");
                        return ValidationResult.Success;
                    }
                },
                new FormField 
                { 
                    Name = "age", 
                    Label = "Age", 
                    Type = FormFieldType.Numeric,
                    MinValue = 18,
                    MaxValue = 120,
                    SimpleValidator = (value) => {
                        var age = Convert.ToDecimal(value);
                        return age >= 18; // Must be 18 or older
                    },
                    SimpleValidatorMessage = "You must be 18 or older"
                }
            }
        };

        var result = await DialogService.OpenFormAsync("Validated Form", options);
        
        if (result != null)
        {
            // Form was submitted with valid data
        }
    }
}
```

## API Reference

### FormDialogOptions

Properties for configuring the dynamic form dialog:

| Property | Type | Description |
|----------|------|-------------|
| `Fields` | `List<FormField>` | List of form fields to display |
| `SaveButtonText` | `string` | Text for the save/submit button |
| `CancelButtonText` | `string` | Text for the cancel button |
| `SaveButtonStyle` | `ButtonStyle` | Style for the save button |
| `CancelButtonStyle` | `ButtonStyle` | Style for the cancel button |
| `Horizontal` | `bool` | Whether fields should be laid out horizontally |
| `FieldSpacing` | `string` | CSS class for spacing between fields |
| `InitialValues` | `Dictionary<string, object>` | Initial values for fields |

### FormField

Properties for configuring individual form fields:

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Unique identifier for the field |
| `Label` | `string` | Display label |
| `Type` | `FormFieldType` | Type of form field |
| `Required` | `bool` | Whether the field is required |
| `DefaultValue` | `object` | Default value if not in InitialValues |
| `RequiredMessage` | `string` | Error message if required field is empty |
| `Placeholder` | `string` | Placeholder text |
| `MinValue` | `object` | Minimum value for numeric/date fields |
| `MaxValue` | `object` | Maximum value for numeric/date fields |
| `Items` | `IEnumerable<object>` | Options for Select/Radio fields |
| `TextProperty` | `string` | Property name for display text in Select/Radio |
| `ValueProperty` | `string` | Property name for value in Select/Radio |
| `CssClass` | `string` | Additional CSS class |
| `TabIndex` | `int?` | Tab index for keyboard navigation |
| `Disabled` | `bool` | Whether the field is disabled |
| `HelpText` | `string` | Help text displayed below the field |
| `Validator` | `Func<object, ValidationResult>` | Advanced validation function |
| `SimpleValidator` | `Func<object, bool>` | Simple validation function |
| `SimpleValidatorMessage` | `string` | Error message for simple validation |

### SimpleInputDialogOptions

Properties for configuring simple input dialogs:

| Property | Type | Description |
|----------|------|-------------|
| `InputType` | `SimpleInputType` | Type of input (Text, Numeric, TextArea) |
| `Label` | `string` | Label for the input field |
| `Placeholder` | `string` | Placeholder text |
| `DefaultValue` | `object` | Default value |
| `Required` | `bool` | Whether input is required |
| `RequiredMessage` | `string` | Error message for required validation |
| `MinValue` | `decimal?` | Minimum value (for numeric inputs) |
| `MaxValue` | `decimal?` | Maximum value (for numeric inputs) |
| `AllowZero` | `bool` | Whether zero is allowed in numeric inputs |
| `Rows` | `int` | Number of rows for textarea |
| `Validator` | `Func<object, bool>` | Custom validation function |
| `ValidationMessage` | `string` | Error message for custom validation |
| `OkButtonText` | `string` | Text for the OK button |
| `CancelButtonText` | `string` | Text for the Cancel button |

### DialogService Extension Methods

| Method | Parameters | Description |
|--------|------------|-------------|
| `OpenFormAsync` | `string title, FormDialogOptions options` | Opens a multi-field form dialog |
| `OpenFormAsync` | `RenderFragment title, FormDialogOptions options` | Opens a form dialog with a complex title |
| `PromptAsync` | `string title, SimpleInputDialogOptions options` | Opens a generic simple input dialog |
| `PromptTextAsync` | `string title, string label, string defaultValue, bool required` | Opens a text input dialog |
| `PromptNumberAsync` | `string title, string label, decimal defaultValue, decimal? minValue, decimal? maxValue, bool required` | Opens a numeric input dialog |
| `PromptTextAreaAsync` | `string title, string label, string defaultValue, int rows, bool required` | Opens a text area input dialog |