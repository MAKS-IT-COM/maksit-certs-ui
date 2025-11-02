import React, { useState, ReactNode } from 'react'

interface TreeNode {
  id: string;
  name: string;
  content?: ReactNode | ((ids: string[]) => ReactNode); // Custom content at each node
  children?: TreeNode[]; // Nested nodes for infinite depth
  defaultCollapsed?: boolean; // Default collapse/expand state
}

interface TreeViewProps {
  data: TreeNode[];
  label?: string;
  colspan?: 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12;
}

const TreeViewComponent: React.FC<TreeViewProps> = (props) => {
  
  const { data, label, colspan = 6 } = props
  
  const [collapsedItems, setCollapsedItems] = useState<Record<string, boolean>>(
    () =>
      data.reduce((acc, node) => {
        const initCollapse = (node: TreeNode, collapsed: Record<string, boolean>): Record<string, boolean> => {
          collapsed[node.id] = node.defaultCollapsed ?? true
          if (node.children) {
            node.children.forEach((child) => initCollapse(child, collapsed))
          }
          return collapsed
        }
        return initCollapse(node, acc)
      }, {})
  )

  const toggleCollapse = (id: string) => {
    setCollapsedItems((prev) => ({
      ...prev,
      [id]: !prev[id],
    }))
  }

  const renderTree = (nodes: TreeNode[], parentIds: string[] = []) => {
    return nodes.map((node) => {
      const nodeIds = [...parentIds, node.id]

      return (
        <div key={node.id} className={'mb-2 ml-4'}>
          {/* Node Header */}
          <div className={'flex items-center justify-between'}>
            <div className={'flex items-center'}>
              <span className={'font-bold'}>{node.name}</span>
            </div>
            <button
              onClick={() => toggleCollapse(node.id)}
              className={'text-sm text-blue-500 focus:outline-none'}
            >
              {collapsedItems[node.id] ? 'Expand' : 'Collapse'}
            </button>
          </div>

          {/* Node Content */}
          {!collapsedItems[node.id] && (
            <>
              {node.content && (
                <div className={'ml-4 mt-2'}>
                  {typeof node.content === 'function' ? node.content(nodeIds) : node.content}
                </div>
              )}
              {node.children && <div className={'ml-4'}>{renderTree(node.children, nodeIds)}</div>}
            </>
          )}
        </div>
      )
    })
  }

  return (
    <div className={`col-span-${colspan}`}>
      {label && <label className={'block text-gray-700 text-sm font-bold mb-2'}>{label}</label>}
      <div className={'border p-4 rounded-md'}>{renderTree(data)}</div>
    </div>
  )
}

export { TreeViewComponent }