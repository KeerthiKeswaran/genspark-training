const themeToggle = document.getElementById('theme-toggle');
const themeIcon = document.getElementById('theme-toggle-icon');
const videoBg = document.querySelector('.video-bg');

themeToggle.addEventListener('click', () => {
  const isLight = document.body.classList.toggle('light-theme');
  if (isLight) {
    themeIcon.className = 'bi bi-sun';
    videoBg.src = 'assets/videos/light-theme.mp4';
    localStorage.setItem('theme', 'light');
  } else {
    themeIcon.className = 'bi bi-moon-stars';
    videoBg.src = 'assets/videos/dark-theme.mp4';
    localStorage.setItem('theme', 'dark');
  }
});

const savedTheme = localStorage.getItem('theme');
if (savedTheme === 'light') {
  document.body.classList.add('light-theme');
  themeIcon.className = 'bi bi-sun';
  videoBg.src = 'assets/videos/light-theme.mp4';
}

document.querySelectorAll('a[href^="#"]').forEach(anchor => {
  anchor.addEventListener('click', function (e) {
    e.preventDefault();
    const targetId = this.getAttribute('href');
    if (targetId === '#') return;
    const targetElement = document.querySelector(targetId);
    if (targetElement) {
      targetElement.scrollIntoView({
        behavior: 'smooth'
      });
    }
  });
});
