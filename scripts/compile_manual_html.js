const fs = require('fs');
const path = require('path');

const projectRoot = path.join(__dirname, '..');
const mdPath = path.join(projectRoot, 'docs', 'manuals', 'MANUAL_DO_USUARIO.md');
const screenshotsDir = path.join(projectRoot, 'docs', 'manuals', 'screenshots');
const outputPath = path.join(projectRoot, 'docs', 'manuals', 'copiar_manual_usuario.html');

console.log('Starting User Manual compilation...');

if (!fs.existsSync(mdPath)) {
  console.error(`Source Markdown file not found at: ${mdPath}`);
  process.exit(1);
}

let mdContent = fs.readFileSync(mdPath, 'utf8');

// Replace custom centered figures blocks with base64 embedded images
const figureRegex = /<div align="center">\s*<p>(Figura \d+(?:\.\d+)? - [^<]+)<\/p>\s*<img\s+src="screenshots\/([^"]+)"\s+alt="[^"]+"\s+width="90%"\s*\/?>\s*<p>(Fonte: Produzido pelo autor\.)<\/p>\s*<\/div>/gi;

let replacedCount = 0;
mdContent = mdContent.replace(figureRegex, (match, title, filename, caption) => {
  const filePath = path.join(screenshotsDir, filename);
  if (fs.existsSync(filePath)) {
    const fileBase64 = fs.readFileSync(filePath, 'base64');
    replacedCount++;
    return `
<div class="figure-container">
  <div class="figure-title">${title}</div>
  <img class="figure-image" src="data:image/png;base64,${fileBase64}" alt="${title}" />
  <div class="figure-caption">${caption}</div>
</div>`;
  } else {
    console.warn(`File not found: ${filePath}`);
    return match;
  }
});

console.log(`Successfully embedded ${replacedCount} base64 images.`);

// Parse Markdown content to HTML
const lines = mdContent.split(/\r?\n/);
let htmlBlocks = [];
let inList = null; // 'ul', 'ol', or null
let inCodeBlock = false;
let codeBlockContent = [];

for (let i = 0; i < lines.length; i++) {
  let line = lines[i];

  // Code blocks
  if (line.trim().startsWith('```')) {
    if (inCodeBlock) {
      htmlBlocks.push(`<pre><code>${codeBlockContent.join('\n')}</code></pre>`);
      inCodeBlock = false;
      codeBlockContent = [];
    } else {
      inCodeBlock = true;
    }
    continue;
  }

  if (inCodeBlock) {
    const escaped = line
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;');
    codeBlockContent.push(escaped);
    continue;
  }

  // Check if we need to close lists
  const isBulletList = line.trim().startsWith('- ') || line.trim().startsWith('* ');
  const isNumberedList = /^\d+\.\s/.test(line.trim());

  if (inList === 'ul' && !isBulletList) {
    htmlBlocks.push('</ul>');
    inList = null;
  } else if (inList === 'ol' && !isNumberedList) {
    htmlBlocks.push('</ol>');
    inList = null;
  }

  // Headings
  if (line.startsWith('# ')) {
    const text = line.substring(2);
    htmlBlocks.push(`<h1 id="${slugify(text)}">${parseInline(text)}</h1>`);
  } else if (line.startsWith('## ')) {
    const text = line.substring(3);
    htmlBlocks.push(`<h2 id="${slugify(text)}">${parseInline(text)}</h2>`);
  } else if (line.startsWith('### ')) {
    const text = line.substring(4);
    htmlBlocks.push(`<h3 id="${slugify(text)}">${parseInline(text)}</h3>`);
  } else if (line.startsWith('#### ')) {
    const text = line.substring(5);
    htmlBlocks.push(`<h4 id="${slugify(text)}">${parseInline(text)}</h4>`);
  } else if (line.trim() === '---') {
    htmlBlocks.push('<hr/>');
  } else if (isBulletList) {
    if (inList !== 'ul') {
      htmlBlocks.push('<ul>');
      inList = 'ul';
    }
    const listContent = line.trim().substring(2);
    htmlBlocks.push(`<li>${parseInline(listContent)}</li>`);
  } else if (isNumberedList) {
    if (inList !== 'ol') {
      htmlBlocks.push('<ol>');
      inList = 'ol';
    }
    const listContent = line.trim().replace(/^\d+\.\s+/, '');
    htmlBlocks.push(`<li>${parseInline(listContent)}</li>`);
  } else if (line.trim().startsWith('<')) {
    // Keep any raw HTML lines (tables, divs) as-is
    htmlBlocks.push(line);
  } else if (line.trim() === '') {
    // Empty line
  } else {
    // Regular paragraph
    htmlBlocks.push(`<p>${parseInline(line)}</p>`);
  }
}

// Close open lists
if (inList === 'ul') htmlBlocks.push('</ul>');
if (inList === 'ol') htmlBlocks.push('</ol>');

function parseInline(text) {
  return text
    // Bold
    .replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>')
    .replace(/__([^_]+)__/g, '<strong>$1</strong>')
    // Italics
    .replace(/\*([^*]+)\*/g, '<em>$1</em>')
    .replace(/_([^_]+)_/g, '<em>$1</em>')
    // Inline code
    .replace(/`([^`]+)`/g, '<code>$1</code>')
    // Link file:// or web URL
    .replace(/\[([^\]]+)\]\(([^)]+)\)/g, (match, linkText, url) => {
      return `<a href="${url}">${linkText}</a>`;
    });
}

function slugify(text) {
  let cleanText = text.replace(/<[^>]+>/g, '');
  return cleanText
    .toLowerCase()
    .replace(/[.,:;?!()"`']/g, '') // Remove punctuation
    .trim()
    .replace(/\s+/g, '-') // Replace spaces with dashes
    .replace(/-+/g, '-');
}

// Render finalized HTML template
const finalHtml = `<!DOCTYPE html>
<html lang="pt-BR">
<head>
  <meta charset="UTF-8">
  <title>Manual do Usuário - Plataforma Rail Factory - Cópia</title>
  <style>
    body {
      font-family: 'Arial', 'Calibri', sans-serif;
      line-height: 1.6;
      max-width: 900px;
      margin: 40px auto;
      padding: 20px;
      color: #333333;
    }
    h1, h2, h3, h4 {
      color: #0078d4;
      margin-top: 1.8em;
      margin-bottom: 0.6em;
      font-weight: bold;
    }
    h1 {
      border-bottom: 2px solid #0078d4;
      padding-bottom: 8px;
      font-size: 28px;
    }
    h2 {
      border-bottom: 1px solid #e0e0e0;
      padding-bottom: 6px;
      font-size: 22px;
    }
    h3 {
      font-size: 18px;
    }
    p, li {
      font-size: 15px;
      margin-bottom: 12px;
      text-align: justify;
    }
    ul, ol {
      margin-left: 20px;
      margin-bottom: 20px;
    }
    code {
      font-family: 'Consolas', 'Courier New', monospace;
      background-color: #f4f4f4;
      padding: 2px 5px;
      border-radius: 3px;
      font-size: 14px;
    }
    pre {
      background-color: #f4f4f4;
      padding: 15px;
      border-radius: 5px;
      overflow-x: auto;
      border-left: 4px solid #0078d4;
      margin-bottom: 20px;
    }
    pre code {
      padding: 0;
      background-color: transparent;
      border-radius: 0;
    }
    a {
      color: #0078d4;
      text-decoration: none;
    }
    a:hover {
      text-decoration: underline;
    }
    hr {
      border: 0;
      border-top: 1px solid #e0e0e0;
      margin: 30px 0;
    }
    .instruction-banner {
      background-color: #f0f4f8;
      border-left: 4px solid #0078d4;
      padding: 20px;
      margin-bottom: 40px;
      border-radius: 4px;
      font-size: 15px;
    }
    .figure-container {
      margin: 40px 0;
      page-break-inside: avoid;
      text-align: center;
    }
    .figure-title {
      font-size: 14px;
      font-weight: bold;
      margin-bottom: 8px;
      text-align: center;
      color: #333333;
    }
    .figure-image {
      max-width: 90%;
      border: 1px solid #c0c0c0;
      display: block;
      margin: 8px auto;
      box-shadow: 0 4px 10px rgba(0,0,0,0.06);
    }
    .figure-caption {
      font-size: 14px;
      color: #555555;
      margin-top: 8px;
      text-align: center;
    }
  </style>
</head>
<body>

  <div class="instruction-banner">
    <strong>📋 Como copiar o Manual do Usuário Completo para o Word / Google Docs:</strong><br/>
    1. Abra esta página no seu navegador web.<br/>
    2. Pressione <strong>Ctrl + A</strong> para selecionar todo o manual (texto formatado, títulos e listas).<br/>
    3. Pressione <strong>Ctrl + C</strong> para copiar para a área de transferência.<br/>
    4. Abra o seu editor (Microsoft Word ou Google Docs) e pressione <strong>Ctrl + V</strong>.<br/>
    <em>Todo o texto formatado, cabeçalhos, marcadores e estrutura do documento serão importados mantendo o estilo original!</em>
  </div>

  ${htmlBlocks.join('\n')}

</body>
</html>`;

fs.writeFileSync(outputPath, finalHtml);
console.log('Successfully compiled copyable User Manual HTML to:', outputPath);
process.exit(0);
