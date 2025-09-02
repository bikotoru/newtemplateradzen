# ConfirmWait Dialog System Documentation

This documentation explains how to use the Custom Radzen ConfirmWait Dialog System, a specialized dialog component for Blazor applications that adds a countdown feature to confirmation dialogs.

## Table of Contents
- [Overview](#overview)
- [Core Components](#core-components)
- [Basic Usage](#basic-usage)
- [Customization Options](#customization-options)
- [Complete Examples](#complete-examples)
  - [Basic Confirmation with Countdown](#basic-confirmation-with-countdown)
  - [Custom Wait Time](#custom-wait-time)
  - [Rich Content Confirmation](#rich-content-confirmation)
  - [With Busy Indicator](#with-busy-indicator)
- [API Reference](#api-reference)

## Overview

The ConfirmWait Dialog System extends Radzen's standard confirmation dialogs by adding a countdown timer before the confirmation button becomes active. This is particularly useful for:

- Preventing accidental confirmations for destructive actions
- Giving users time to read important information before proceeding
- Forcing users to pause before making significant decisions
- Implementing a "cooling-off" period for critical operations

The system displays a dialog with a message and two buttons: a confirmation button (with countdown) and a cancel button.

## Core Components

The system consists of the following key components:

1. **CountdownButton** - A specialized button component that displays a countdown and remains disabled for a specified number of seconds
2. **ConfirmWaitOptions** - Configuration options for the confirmation dialog
3. **ConfirmWaitDialogServiceExtensions** - Extension methods for the Radzen DialogService

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

### Opening a Basic ConfirmWait Dialog

To open a confirmation dialog with a 5-second countdown (default):

```csharp
bool? result = await DialogService.ConfirmWait(
    "Are you sure you want to delete this item?", 
    "Confirm Delete");

if (result == true)
{
    // User confirmed after waiting
    await DeleteItemAsync();
}
```

## Customization Options

The ConfirmWait dialog can be customized in various ways:

### Custom Wait Time

You can specify a different countdown duration:

```csharp
var options = new ConfirmWaitOptions
{
    WaitSeconds = 10 // 10-second countdown
};

bool? result = await DialogService.ConfirmWait(
    "This operation cannot be undone. Are you sure?", 
    "Warning", 
    options);
```

### Custom Button Text

```csharp
var options = new ConfirmWaitOptions
{
    OkButtonText = "Delete",
    CancelButtonText = "Keep"
};

bool? result = await DialogService.ConfirmWait(
    "Delete this file permanently?", 
    "Confirm Delete", 
    options);
```

### Custom Countdown Format

```csharp
var options = new ConfirmWaitOptions
{
    CountdownFormat = "Wait {0} seconds..."
};

bool? result = await DialogService.ConfirmWait(
    "This action will log you out of all devices.", 
    "Confirm Logout", 
    options);
```

### Busy Indicator During Countdown

```csharp
var options = new ConfirmWaitOptions
{
    ShowBusyIndicator = true
};

bool? result = await DialogService.ConfirmWait(
    "Processing request...", 
    "Please Wait", 
    options);
```

## Complete Examples

### Basic Confirmation with Countdown

```csharp
@page "/example-confirm"
@inject DialogService DialogService

<RadzenButton Text="Delete Item" Click="@ShowDeleteConfirmation" />

@code {
    private async Task ShowDeleteConfirmation()
    {
        bool? result = await DialogService.ConfirmWait(
            "Are you sure you want to delete this item? This action cannot be undone.",
            "Confirm Delete");
            
        if (result == true)
        {
            // User confirmed after waiting
            await DeleteItemAsync();
        }
    }
    
    private async Task DeleteItemAsync()
    {
        // Delete logic here
    }
}
```

### Custom Wait Time

```csharp
@page "/example-custom-wait"
@inject DialogService DialogService

<RadzenButton Text="Format Drive" Click="@ShowFormatConfirmation" />

@code {
    private async Task ShowFormatConfirmation()
    {
        var options = new ConfirmWaitOptions
        {
            WaitSeconds = 10,
            OkButtonText = "Format Now",
            CancelButtonText = "Cancel"
        };
    
        bool? result = await DialogService.ConfirmWait(
            "You are about to format drive C:. All data will be permanently erased.",
            "Critical Warning",
            options);
            
        if (result == true)
        {
            // Format drive
        }
    }
}
```

### Rich Content Confirmation

```csharp
@page "/example-rich-content"
@inject DialogService DialogService

<RadzenButton Text="Show Terms" Click="@ShowTermsConfirmation" />

@code {
    private async Task ShowTermsConfirmation()
    {
        // Create a rich content message with RenderFragment
        RenderFragment message = builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "terms-container");
            
            builder.OpenElement(2, "h4");
            builder.AddContent(3, "Terms and Conditions");
            builder.CloseElement(); // h4
            
            builder.OpenElement(4, "p");
            builder.AddContent(5, "By clicking Accept, you agree to the following terms:");
            builder.CloseElement(); // p
            
            builder.OpenElement(6, "ul");
            
            builder.OpenElement(7, "li");
            builder.AddContent(8, "Your data will be processed according to our privacy policy");
            builder.CloseElement(); // li
            
            builder.OpenElement(9, "li");
            builder.AddContent(10, "You must be at least 18 years old to use this service");
            builder.CloseElement(); // li
            
            builder.OpenElement(11, "li"); 
            builder.AddContent(12, "All transactions are final and non-refundable");
            builder.CloseElement(); // li
            
            builder.CloseElement(); // ul
            
            builder.CloseElement(); // div
        };
        
        var options = new ConfirmWaitOptions
        {
            WaitSeconds = 8,
            OkButtonText = "Accept",
            CancelButtonText = "Decline",
            Width = "500px"
        };
        
        bool? result = await DialogService.ConfirmWait(
            message,
            "Accept Terms",
            options);
            
        if (result == true)
        {
            // User accepted terms
        }
    }
}
```

### With Busy Indicator

```csharp
@page "/example-busy-indicator"
@inject DialogService DialogService

<RadzenButton Text="Deploy Application" Click="@ShowDeployConfirmation" />

@code {
    private async Task ShowDeployConfirmation()
    {
        var options = new ConfirmWaitOptions
        {
            WaitSeconds = 5,
            CountdownFormat = "Preparing ({0})...",
            ShowBusyIndicator = true,
            OkButtonText = "Deploy Now"
        };
        
        bool? result = await DialogService.ConfirmWait(
            "You are about to deploy to production. This will restart all services.",
            "Confirm Deployment",
            options);
            
        if (result == true)
        {
            // Start deployment
        }
    }
}
```

## API Reference

### ConfirmWaitOptions

Properties for configuring the countdown confirmation dialog:

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `WaitSeconds` | `int` | 5 | Number of seconds to wait before enabling the confirm button |
| `CountdownFormat` | `string` | "{0}s" | Format string for the countdown text, where {0} is replaced with the remaining seconds |
| `ShowBusyIndicator` | `bool` | false | Whether to show a busy indicator on the button during countdown |
| `OkButtonText` | `string` | "Ok" | Text for the confirmation button (after countdown) |
| `CancelButtonText` | `string` | "Cancel" | Text for the cancel button |
| `Width` | `string` | "" | Width of the dialog (defaults to Radzen's 600px) |
| `Style` | `string` | "" | Additional CSS styles for the dialog |
| `CssClass` | `string` | "rz-dialog-confirm" | CSS class for the dialog |
| `WrapperCssClass` | `string` | "rz-dialog-wrapper" | CSS class for the dialog wrapper |

### CountdownButton Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Text` | `string` | "Confirm" | Button text after countdown completes |
| `WaitSeconds` | `int` | 5 | Seconds to wait before enabling the button |
| `CountdownFormat` | `string` | "{0}s" | Format string for countdown display |
| `Click` | `EventCallback<MouseEventArgs>` | - | Event callback for button click |
| `Style` | `string` | "margin-bottom: 10px; width: 150px" | CSS style for the button |
| `ButtonStyle` | `ButtonStyle` | `ButtonStyle.Primary` | Radzen button style |
| `ShowBusyIndicator` | `bool` | false | Whether to show a busy indicator during countdown |
| `Variant` | `Variant` | `Variant.Filled` | Radzen button variant |
| `Icon` | `string` | null | Icon to display on the button |
| `Size` | `ButtonSize` | `ButtonSize.Medium` | Radzen button size |

### DialogService Extension Methods

| Method | Parameters | Description |
|--------|------------|-------------|
| `ConfirmWait` | `string message, string title, ConfirmWaitOptions options, CancellationToken? cancellationToken` | Opens a confirm dialog with string message and countdown |
| `ConfirmWait` | `RenderFragment message, string title, ConfirmWaitOptions options, CancellationToken? cancellationToken` | Opens a confirm dialog with rich content and countdown |

Both methods return `Task<bool?>` where:
- `true` indicates the user confirmed after the countdown
- `false` indicates the user clicked cancel