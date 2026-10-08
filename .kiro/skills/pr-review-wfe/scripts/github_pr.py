#!/usr/bin/env python3
"""
GitHub PR 数据获取脚本（基于 gh CLI）
依赖：本地已通过 `gh auth login` 登录，无需额外配置 Token。

用法:
  python github_pr.py --pr-url <URL> --action info
  python github_pr.py --pr-url <URL> --action diffstat
  python github_pr.py --pr-url <URL> --action diff
  python github_pr.py --pr-url <URL> --action file-diff --file <path>
  python github_pr.py --pr-url <URL> --action comments

支持的 PR URL 格式:
  https://github.com/{owner}/{repo}/pull/{number}
"""

import argparse
import json
import re
import subprocess
import sys


def check_gh_cli():
    """检查 gh CLI 是否可用且已登录"""
    # 检查 gh 命令是否存在
    # Windows 下必须 shell=True，否则 subprocess 无法从 PATH 解析 gh.exe
    result = subprocess.run(
        "gh --version",
        capture_output=True, text=True, encoding="utf-8", errors="replace", shell=True
    )
    if result.returncode != 0:
        print("错误: 未找到 gh CLI。", file=sys.stderr)
        print("请安装 GitHub CLI: https://cli.github.com/", file=sys.stderr)
        print("安装后重启终端，再重试。", file=sys.stderr)
        sys.exit(1)

    # 检查是否已登录
    result = subprocess.run(
        "gh auth status",
        capture_output=True, text=True, encoding="utf-8", errors="replace", shell=True
    )
    if result.returncode != 0:
        print("错误: gh CLI 未登录。", file=sys.stderr)
        print("请执行: gh auth login", file=sys.stderr)
        sys.exit(1)


def parse_pr_url(pr_url):
    """从 PR URL 中解析 owner/repo/pr_number"""
    pattern = r"https://github\.com/([^/]+)/([^/]+)/pull/(\d+)"
    m = re.match(pattern, pr_url.strip())
    if not m:
        print(f"错误: 无法解析 PR URL: {pr_url}", file=sys.stderr)
        print("期望格式: https://github.com/owner/repo/pull/123", file=sys.stderr)
        sys.exit(1)
    return m.group(1), m.group(2), int(m.group(3))


def gh_run(args, check=True):
    """运行 gh 命令，返回 stdout 字符串"""
    # Windows 下必须 shell=True，否则无法从 PATH 找到 gh.exe
    cmd = "gh " + " ".join(f'"{a}"' if " " in str(a) else str(a) for a in args)
    result = subprocess.run(
        cmd,
        capture_output=True, text=True, encoding="utf-8", errors="replace", shell=True
    )
    if check and result.returncode != 0:
        print(f"gh 命令失败: {cmd}", file=sys.stderr)
        print(result.stderr, file=sys.stderr)
        sys.exit(1)
    return result.stdout


def action_info(owner, repo, pr_number):
    """获取 PR 基本信息"""
    fields = "number,title,state,author,headRefName,baseRefName,additions,deletions,changedFiles,createdAt,updatedAt,body,mergeable"
    raw = gh_run([
        "pr", "view", str(pr_number),
        "--repo", f"{owner}/{repo}",
        "--json", fields
    ])
    data = json.loads(raw)

    print(f"PR #{data['number']}: {data['title']}")
    print(f"状态: {data['state']}")
    print(f"作者: {data['author']['login']}")
    print(f"源分支: {data['headRefName']}  →  目标分支: {data['baseRefName']}")
    print(f"可合并: {data.get('mergeable', '未知')}")
    print(f"新增行: {data.get('additions', 'N/A')}  删除行: {data.get('deletions', 'N/A')}  变更文件: {data.get('changedFiles', 'N/A')}")
    print(f"创建时间: {data['createdAt']}")
    print(f"更新时间: {data['updatedAt']}")
    if data.get("body"):
        print()
        print("描述:")
        print(data["body"])


def action_diffstat(owner, repo, pr_number):
    """获取变更文件列表"""
    raw = gh_run([
        "pr", "view", str(pr_number),
        "--repo", f"{owner}/{repo}",
        "--json", "files,additions,deletions,changedFiles"
    ])
    data = json.loads(raw)
    files = data.get("files", [])

    print(f"变更文件共 {data.get('changedFiles', len(files))} 个  (+{data.get('additions', 0)} / -{data.get('deletions', 0)})")
    print()
    for f in files:
        # additions/deletions 可能在 file 对象里，也可能不在（取决于 gh 版本）
        add = f.get("additions", "?")
        delete = f.get("deletions", "?")
        status_map = {"added": "A", "removed": "D", "modified": "M", "renamed": "R", "copied": "C"}
        status = status_map.get(f.get("changeType", "").lower(), "M")
        print(f"  [{status}] {f['path']}  (+{add} / -{delete})")


def action_diff(owner, repo, pr_number):
    """获取完整 diff"""
    output = gh_run([
        "pr", "diff", str(pr_number),
        "--repo", f"{owner}/{repo}"
    ])
    print(output)


def action_file_diff(owner, repo, pr_number, file_path):
    """获取指定文件的 diff（从完整 diff 中过滤）"""
    if not file_path:
        print("错误: --file 参数不能为空", file=sys.stderr)
        sys.exit(1)

    full_diff = gh_run([
        "pr", "diff", str(pr_number),
        "--repo", f"{owner}/{repo}"
    ])

    # 从 unified diff 中提取指定文件的部分
    lines = full_diff.splitlines(keepends=True)
    in_file = False
    result_lines = []

    for line in lines:
        if line.startswith("diff --git"):
            # 检查是否是目标文件（diff --git a/path b/path）
            in_file = f" b/{file_path}" in line or line.strip().endswith(file_path)
        if in_file:
            result_lines.append(line)
            # 遇到下一个 diff 块时停止
            if line.startswith("diff --git") and result_lines and len(result_lines) > 1:
                result_lines.pop()  # 去掉下一个文件的第一行
                break

    if result_lines:
        print("".join(result_lines))
    else:
        print(f"错误: 在 PR 中未找到文件: {file_path}", file=sys.stderr)
        print("提示: 文件路径应为相对于仓库根目录的路径，如 src/app/views/workflow/index.vue", file=sys.stderr)
        sys.exit(1)


def action_comments(owner, repo, pr_number):
    """获取 PR 评论"""
    # PR 级别评论 + review 评论
    raw = gh_run([
        "pr", "view", str(pr_number),
        "--repo", f"{owner}/{repo}",
        "--json", "comments,reviews"
    ])
    data = json.loads(raw)

    comments = data.get("comments", [])
    reviews = data.get("reviews", [])

    print(f"=== PR 评论 ({len(comments)} 条) ===")
    for c in comments:
        author = c.get("author", {}).get("login", "unknown")
        print(f"\n[{author}] {c.get('createdAt', '')}")
        print(c.get("body", ""))

    print(f"\n=== Review 评论 ({len(reviews)} 条) ===")
    for r in reviews:
        author = r.get("author", {}).get("login", "unknown")
        state = r.get("state", "")
        print(f"\n[{author}] {state}  {r.get('submittedAt', '')}")
        if r.get("body"):
            print(r["body"])


def main():
    parser = argparse.ArgumentParser(description="GitHub PR 数据获取工具（基于 gh CLI）")
    parser.add_argument("--pr-url", required=True, help="GitHub PR URL")
    parser.add_argument("--action", required=True,
                        choices=["info", "diffstat", "diff", "file-diff", "comments"],
                        help="执行的操作")
    parser.add_argument("--file", help="file-diff 操作时指定的文件路径（相对于仓库根目录）")
    args = parser.parse_args()

    check_gh_cli()
    owner, repo, pr_number = parse_pr_url(args.pr_url)

    if args.action == "info":
        action_info(owner, repo, pr_number)
    elif args.action == "diffstat":
        action_diffstat(owner, repo, pr_number)
    elif args.action == "diff":
        action_diff(owner, repo, pr_number)
    elif args.action == "file-diff":
        action_file_diff(owner, repo, pr_number, args.file)
    elif args.action == "comments":
        action_comments(owner, repo, pr_number)


if __name__ == "__main__":
    main()
