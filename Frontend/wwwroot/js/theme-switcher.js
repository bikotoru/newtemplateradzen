window.switchTheme = (cssFile) => {
    console.log('switchTheme called with:', cssFile);
    
    try {
        // Debug: List all link tags
        const allLinks = document.querySelectorAll('link[rel="stylesheet"]');
        console.log('All CSS links found:', Array.from(allLinks).map(l => l.href));
        
        // Find the existing theme link
        const existingThemeLink = document.querySelector('link[data-light="true"]') || 
            document.querySelector('link[href*="/style/fluent-base.css"]') ||
            document.querySelector('link[href*="/style/fluent-dark-base.css"]');
        
        if (existingThemeLink) {
            console.log('Found existing theme link:', existingThemeLink.href);
            // Change the href to switch themes
            const oldHref = existingThemeLink.href;
            existingThemeLink.href = cssFile;
            console.log('Changed theme link from:', oldHref, 'to:', existingThemeLink.href);
            
            // Verify the change
            setTimeout(() => {
                console.log('Current theme link after change:', existingThemeLink.href);
            }, 100);
            
        } else {
            console.log('No existing theme link found, creating new one');
            // Create new theme link if none exists
            const newThemeLink = document.createElement('link');
            newThemeLink.rel = 'stylesheet';
            newThemeLink.href = cssFile;
            newThemeLink.setAttribute('data-light', 'true');
            document.head.appendChild(newThemeLink);
            console.log('Added new theme link:', cssFile);
        }
        
    } catch (error) {
        console.error('Error in switchTheme:', error);
    }
};

// Set theme function
window.setTheme = (theme) => {
    console.log('Setting theme to:', theme);
    localStorage.setItem('fluentTheme', theme);
    const cssFile = theme === 'dark' ? '/style/fluent-dark-base.css' : '/style/fluent-base.css';
    window.switchTheme(cssFile);
};

// Initialize theme on page load
window.initializeTheme = () => {
    console.log('Initializing theme...');
    try {
        const savedTheme = localStorage.getItem('fluentTheme') || 'light';
        console.log('Saved theme from localStorage:', savedTheme);
        const cssFile = savedTheme === 'dark' ? '/style/fluent-dark-base.css' : '/style/fluent-base.css';
        console.log(cssFile);
        window.switchTheme(cssFile);
    } catch (error) {
        console.error('Error initializing theme:', error);
    }
};