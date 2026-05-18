---
name: Item Components
description: 'Item EAG UI component library. Table with resizable columns, Loading/WaveLoading, shadcn/ui component usage patterns. Actions: build component, create UI, implement table, add loading state, use shadcn components.'
inclusion: manual
role: Senior Frontend Architect
priority: high
---

# Item Components — shadcn/ui Wrapping Protocol

## 🔍 Step 0: Check Project First (MANDATORY)

Before writing any component code, always check:

```
Does src/app/components/ui/ exist in this project?
        │
   YES  ├─→ Check the inventory list below
        │         │
        │    Found ├─→ Import and use directly — STOP, do not recreate
        │          │
        │    Not found ─→ Follow "How to Wrap" section below
        │
   NO   └─→ Follow "Setting Up from Scratch" section below
```

---

## ✅ Scenario A: Project Already Has Component Structure

If `src/app/components/ui/` exists, these components are available. Import and use directly:

### Basic

| Component    | Import                        | Notes                   |
| :----------- | :---------------------------- | :---------------------- |
| `Button`     | `@/components/ui/button`      | variant, size, disabled |
| `Input`      | `@/components/ui/input`       | standard text input     |
| `Label`      | `@/components/ui/label`       | form labels             |
| `Badge`      | `@/components/ui/badge`       | status tags             |
| `Avatar`     | `@/components/ui/avatar`      | user avatar             |
| `Separator`  | `@/components/ui/separator`   | divider line            |
| `Skeleton`   | `@/components/ui/skeleton`    | loading placeholder     |
| `Tooltip`    | `@/components/ui/tooltip`     | hover hint              |
| `Progress`   | `@/components/ui/progress`    | progress bar            |
| `Slider`     | `@/components/ui/slider`      | range input             |
| `Switch`     | `@/components/ui/switch`      | toggle                  |
| `Checkbox`   | `@/components/ui/checkbox`    | boolean input           |
| `RadioGroup` | `@/components/ui/radio-group` | single selection        |

### Layout & Container

| Component       | Import                           | Notes              |
| :-------------- | :------------------------------- | :----------------- |
| `Card`          | `@/components/ui/card`           | content container  |
| `Collapsible`   | `@/components/ui/collapsible`    | expand/collapse    |
| `Collapse`      | `@/components/ui/collapse`       | animated collapse  |
| `ScrollArea`    | `@/components/ui/scroll-area`    | custom scrollbar   |
| `Tabs`          | `@/components/ui/tabs`           | tab navigation     |
| `PrototypeTabs` | `@/components/ui/prototype-tabs` | styled tab variant |

### Data Display

| Component        | Import                            | Notes                            |
| :--------------- | :-------------------------------- | :------------------------------- |
| `Table`          | `@/components/ui/table`           | resizable columns, sticky header |
| `Pagination`     | `@/components/ui/pagination`      | page navigation                  |
| `Timeline`       | `@/components/ui/timeline`        | event timeline                   |
| `TimeLine`       | `@/components/ui/time-line`       | time-based variant               |
| `Empty`          | `@/components/ui/empty`           | empty state                      |
| `CountTo`        | `@/components/ui/count-to`        | animated number                  |
| `UDetailInfo`    | `@/components/ui/u-detail-info`   | key-value detail display         |
| `UHeading`       | `@/components/ui/u-heading`       | section heading                  |
| `GradientBadge`  | `@/components/ui/gradient-badge`  | gradient styled badge            |
| `AvatarUsername` | `@/components/ui/avatar-username` | avatar + name combo              |

### Form & Input

| Component          | Import                              | Notes                       |
| :----------------- | :---------------------------------- | :-------------------------- |
| `Select`           | `@/components/ui/select`            | dropdown select             |
| `MultiSelect`      | `@/components/ui/multi-select`      | multi-value select          |
| `SearchableSelect` | `@/components/ui/searchable-select` | select with search          |
| `TreeSelect`       | `@/components/ui/tree-select`       | hierarchical select         |
| `DatePicker`       | `@/components/ui/date-picker`       | single date                 |
| `DateRangePicker`  | `@/components/ui/date-range-picker` | date range                  |
| `ColorPicker`      | `@/components/ui/color-picker`      | color input                 |
| `InputNumber`      | `@/components/ui/input-number`      | numeric input with controls |
| `InputDiscount`    | `@/components/ui/input-discount`    | discount/percentage input   |
| `PhoneInput`       | `@/components/ui/phone-input`       | phone with country code     |
| `AuthCode`         | `@/components/ui/auth-code`         | OTP / verification code     |
| `TagInput`         | `@/components/ui/tag-input`         | free-form tag input         |
| `UInputTags`       | `@/components/ui/u-input-tags`      | structured tag input        |
| `FileUpload`       | `@/components/ui/file-upload`       | file/image upload           |
| `Calendar`         | `@/components/ui/calendar`          | calendar picker             |

### Overlay & Feedback

| Component       | Import                           | Notes                 |
| :-------------- | :------------------------------- | :-------------------- |
| `Dialog`        | `@/components/ui/dialog`         | modal dialog          |
| `ConfirmDialog` | `@/components/ui/confirm-dialog` | confirmation modal    |
| `Drawer`        | `@/components/ui/drawer`         | side panel            |
| `Popover`       | `@/components/ui/popover`        | floating content      |
| `DropdownMenu`  | `@/components/ui/dropdown-menu`  | action menu           |
| `Command`       | `@/components/ui/command`        | command palette       |
| `Alert`         | `@/components/ui/alert`          | inline alert          |
| `Toaster`       | `@/components/ui/toaster`        | toast notifications   |
| `Loading`       | `@/components/ui/loading`        | wave animation loader |
| `Breadcrumb`    | `@/components/ui/breadcrumb`     | navigation breadcrumb |
| `Steps`         | `@/components/ui/steps`          | step indicator        |

### People & User

| Component        | Import                             | Notes                    |
| :--------------- | :--------------------------------- | :----------------------- |
| `PeopleSelect`   | `@/components/ui/people-select`    | user/people picker       |
| `PeopleTags`     | `@/components/ui/people-tags`      | people tag display       |
| `TeamUserSelect` | `@/components/ui/team-user-select` | team member selector     |
| `UserSelect`     | `@/components/ui/user-select`      | single user picker       |
| `PageHeader`     | `@/components/ui/page-header`      | page title + actions bar |

---

## 🏗️ Scenario B: Project Has No Component Structure (Setup from Scratch)

If `src/app/components/ui/` does not exist, set it up following these steps:

### 1. Install shadcn/ui

```bash
npx shadcn@latest init
```

Choose: TypeScript, Tailwind CSS, `@/components` as alias.

### 2. Create the directory structure

```
src/app/components/
└── ui/
    └── [component-name]/
        └── index.tsx     ← one folder per component, always index.tsx
```

### 3. Add a shadcn component

```bash
npx shadcn@latest add button
npx shadcn@latest add input dialog table ...
```

Then move/wrap it under `src/app/components/ui/[name]/index.tsx`.

### 4. Wrapping convention

Every component must follow this pattern:

```tsx
// src/app/components/ui/[name]/index.tsx
import * as React from 'react';
import { cn } from '@/utils/cn';
// import the shadcn-generated component or radix primitive

export interface MyComponentProps extends React.ComponentPropsWithoutRef<'div'> {
	// add project-specific props here
}

const MyComponent = React.forwardRef<HTMLDivElement, MyComponentProps>(
	({ className, ...props }, ref) => (
		<div ref={ref} className={cn('base-classes', 'dark:dark-classes', className)} {...props} />
	)
);
MyComponent.displayName = 'MyComponent';

export { MyComponent };
```

Rules:

-   Always `cn()` from `@/utils/cn` for class merging
-   Always accept and forward `className`
-   Always include `dark:` variants
-   Always `React.forwardRef` for DOM elements
-   Named exports only — no default exports
-   No business logic inside wrappers

### 5. If shadcn doesn't have the component

Compose from existing shadcn primitives:

```tsx
// e.g. SearchableSelect = Popover + Command
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover';
import { Command, CommandInput, CommandList, CommandItem } from '@/components/ui/command';
```

---

## 📐 Usage Rule (Both Scenarios)

```tsx
// ✅ Always import from @/components/ui/
import { Button } from '@/components/ui/button';
import {
	Table,
	TableHeader,
	TableBody,
	TableHead,
	TableRow,
	TableCell,
} from '@/components/ui/table';

// ❌ Never import raw Radix or shadcn primitives in feature/view code
import * as Dialog from '@radix-ui/react-dialog';
```

---

## 📋 Typical Feature Page

```tsx
import React, { useState } from 'react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import {
	Table,
	TableHeader,
	TableBody,
	TableHead,
	TableRow,
	TableCell,
} from '@/components/ui/table';
import { Loading } from '@/components/ui/loading';
import { PageHeader } from '@/components/ui/page-header';
import { Icon } from '@iconify/react';

const FeaturePage: React.FC = () => {
	const [loading, setLoading] = useState(false);
	if (loading) return <Loading fullscreen text="Loading..." />;

	return (
		<div className="flex flex-col gap-4 p-6 dark:bg-gray-900">
			<PageHeader title="Feature Title" />
			<div className="flex gap-2">
				<Input placeholder="Search..." className="max-w-xs" />
				<Button className="bg-[#6B46C1] hover:bg-[#5a37a8]">
					<Icon icon="lucide:plus" className="mr-2 h-4 w-4" />
					Add New
				</Button>
			</div>
			<Table maxHeight="calc(100vh - 280px)">
				<TableHeader>
					<TableRow>
						<TableHead>Name</TableHead>
						<TableHead>Status</TableHead>
						<TableHead>Actions</TableHead>
					</TableRow>
				</TableHeader>
				<TableBody>{/* rows */}</TableBody>
			</Table>
		</div>
	);
};

export default FeaturePage;
```

---

## 🎨 Tailwind Utilities Reference

#[[file:.kiro/steering/item-components/reference/tailwind-utilities.csv]]

---

## 🎭 Common Icons Reference

#[[file:.kiro/steering/item-components/reference/common-icons.csv]]
