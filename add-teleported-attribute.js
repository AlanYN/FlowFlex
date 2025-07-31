const fs = require('fs');
const path = require('path');
const { execSync } = require('child_process');

// 递归查找包含el-select的Vue文件
function findVueFiles(dir, fileList = []) {
  const files = fs.readdirSync(dir);
  
  files.forEach(file => {
    const filePath = path.join(dir, file);
    const stat = fs.statSync(filePath);
    
    if (stat.isDirectory()) {
      findVueFiles(filePath, fileList);
    } else if (file.endsWith('.vue')) {
      const content = fs.readFileSync(filePath, 'utf-8');
      if (content.includes('<el-select')) {
        fileList.push(filePath);
      }
    }
  });
  
  return fileList;
}

// 处理单个文件
const processFile = (filePath) => {
  try {
    console.log(`Processing file: ${filePath}`);
    let content = fs.readFileSync(filePath, 'utf-8');
    
    // 正则表达式匹配el-select开始标签
    const regex = /<el-select(?!\s[^>]*:teleported="false")([^>]*)>/g;
    
    // 检查文件是否包含需要修改的el-select
    if (!regex.test(content)) {
      console.log(`  No changes needed for ${filePath}`);
      return false;
    }
    
    // 重置正则表达式状态
    regex.lastIndex = 0;
    
    // 替换所有匹配项
    let newContent = content.replace(regex, (match, attributes) => {
      // 如果属性在多行，我们需要在最后一行添加:teleported="false"
      if (attributes.includes('\n')) {
        // 查找最后一个属性的位置
        const lines = attributes.split('\n');
        const lastLine = lines[lines.length - 1];
        const indentation = lastLine.match(/^\s*/)[0];
        
        // 在最后一行后添加:teleported="false"
        lines[lines.length - 1] = `${lastLine}\n${indentation}:teleported="false"`;
        return `<el-select${lines.join('\n')}>`;
      } else {
        // 单行情况，直接添加属性
        return `<el-select${attributes} :teleported="false">`;
      }
    });
    
    // 写回文件
    fs.writeFileSync(filePath, newContent, 'utf-8');
    console.log(`  Updated ${filePath}`);
    return true;
  } catch (error) {
    console.error(`Error processing file ${filePath}:`, error);
    return false;
  }
};

// 主函数
const main = () => {
  const rootDir = path.join(process.cwd(), 'packages', 'flowFlex-common', 'src');
  console.log(`Searching for Vue files in: ${rootDir}`);
  
  const files = findVueFiles(rootDir);
  console.log(`Found ${files.length} files with el-select components`);
  
  let updatedCount = 0;
  files.forEach(file => {
    if (processFile(file)) {
      updatedCount++;
    }
  });
  
  console.log(`Updated ${updatedCount} files with el-select components`);
};

main(); 