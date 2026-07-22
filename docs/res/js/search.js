/* Quark docs — buscador global (command palette · Ctrl K)
   Índice central: crece con la docu. Cada entry: t título · c contexto · u url (desde docs/) · k keywords · y tipo · img opcional */

const QROOT = location.pathname.replace(/\\/g, '/').includes('/res/') ? '../' : '';

const QINDEX = [
  /* páginas */
  { t: 'Overview', c: 'documentación', u: 'index.html', k: 'inicio referencia licencia', y: 'página' },
  { t: 'Design', c: 'fundamentos', u: 'res/design.html', k: 'principios arquitectura normativo', y: 'página' },
  { t: 'Style', c: 'fundamentos', u: 'res/style.html', k: 'estilo codigo regiones workflow', y: 'página' },
  { t: 'Palettes', c: 'recursos', u: 'res/palettes.html', k: 'paleta colores hex brand atomos', y: 'página' },
  { t: 'Controls', c: 'recursos', u: 'res/controls.html', k: 'controles bindings teclas input', y: 'página' },
  { t: 'Input Icons', c: 'recursos', u: 'res/icons.html', k: 'iconos sprites palette keys', y: 'página' },

  /* secciones — design */
  { t: 'Filosofía', c: 'Design', u: 'res/design.html#filosofia', k: 'principios reglas yagni lazy reusar componer herencia null comentarios one-liner', y: 'sección' },
  { t: 'Arquitectura', c: 'Design', u: 'res/design.html#arquitectura', k: 'entity prop service identifiable addon mod nucleus values gamemanager atom bases vocabulario', y: 'sección' },
  { t: 'Proyecto', c: 'Design', u: 'res/design.html#proyecto', k: 'root core extensions capas carpetas ciclo vida hook handle unhook library resources source', y: 'sección' },
  { t: 'Roadmap', c: 'Design', u: 'res/design.html#roadmap', k: 'settings services packaging asmdef vision', y: 'sección' },

  /* secciones — style */
  { t: 'Regiones', c: 'Style', u: 'res/style.html#regiones', k: 'fields lifetime api misc', y: 'sección' },
  { t: 'Prácticas', c: 'Style', u: 'res/style.html#reglas', k: 'inspector foldouts values api nested', y: 'sección' },
  { t: 'Workflow', c: 'Style', u: 'res/style.html#workflow', k: 'commits genesis release ramas', y: 'sección' },
  { t: 'Documentación', c: 'Style', u: 'res/style.html#docs', k: 'docpass summary indice buscador', y: 'sección' },

  /* controles */
  { t: 'Move', c: 'WASD · Left Stick', u: 'res/controls.html', k: 'mover caminar wasd leftstick', y: 'control' },
  { t: 'Look', c: 'Mouse · Right Stick', u: 'res/controls.html', k: 'camara mirar delta rightstick', y: 'control' },
  { t: 'Jump', c: 'Space · A', u: 'res/controls.html', k: 'saltar space buttonsouth', y: 'control' },
  { t: 'Sprint', c: 'Shift · L3', u: 'res/controls.html', k: 'correr shift leftstickpress', y: 'control' },
  { t: 'Crouch', c: 'C · R3', u: 'res/controls.html', k: 'agacharse toggle rightstickpress', y: 'control' },
  { t: 'CrouchHold', c: 'Ctrl', u: 'res/controls.html', k: 'agacharse hold', y: 'control' },
  { t: 'Interact', c: 'E · X', u: 'res/controls.html', k: 'interactuar usar buttonwest', y: 'control' },

  /* íconos (generado) */
  // @icons
  { t: '0', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/0.png' },
  { t: '1', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/1.png' },
  { t: '2', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/2.png' },
  { t: '3-1', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/3-1.png' },
  { t: '3', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/3.png' },
  { t: '4', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/4.png' },
  { t: '5', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/5.png' },
  { t: '6', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/6.png' },
  { t: '7', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/7.png' },
  { t: '8', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/8.png' },
  { t: '9', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/9.png' },
  { t: 'Backspace_Alt', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/Backspace_Alt.png' },
  { t: 'Enter_Alt', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/Enter_Alt.png' },
  { t: 'Enter_Tall', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/Enter_Tall.png' },
  { t: 'Keyboard_R-1', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/Keyboard_R-1.png' },
  { t: 'Keyboard_R', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/Keyboard_R.png' },
  { t: 'Plus_Tall', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/Plus_Tall.png' },
  { t: 'Shift_Super_Wide', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/Shift_Super_Wide.png' },
  { t: 'a', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/a.png' },
  { t: 'alt', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/alt.png' },
  { t: 'b', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/b.png' },
  { t: 'backquote', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/backquote.png' },
  { t: 'backspace', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/backspace.png' },
  { t: 'c', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/c.png' },
  { t: 'capsLock', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/capsLock.png' },
  { t: 'ctrl', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/ctrl.png' },
  { t: 'cursor', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/cursor.png' },
  { t: 'd', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/d.png' },
  { t: 'delete', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/delete.png' },
  { t: 'delta', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/delta.png' },
  { t: 'downArrow', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/downArrow.png' },
  { t: 'e', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/e.png' },
  { t: 'end', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/end.png' },
  { t: 'enter', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/enter.png' },
  { t: 'equals', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/equals.png' },
  { t: 'escape', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/escape.png' },
  { t: 'f', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/f.png' },
  { t: 'f1', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/f1.png' },
  { t: 'f10', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/f10.png' },
  { t: 'f11', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/f11.png' },
  { t: 'f12', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/f12.png' },
  { t: 'f2', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/f2.png' },
  { t: 'f3', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/f3.png' },
  { t: 'f4', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/f4.png' },
  { t: 'f5', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/f5.png' },
  { t: 'f6', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/f6.png' },
  { t: 'f7', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/f7.png' },
  { t: 'f8', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/f8.png' },
  { t: 'f9', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/f9.png' },
  { t: 'g', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/g.png' },
  { t: 'h', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/h.png' },
  { t: 'home', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/home.png' },
  { t: 'i', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/i.png' },
  { t: 'insert', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/insert.png' },
  { t: 'j', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/j.png' },
  { t: 'k', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/k.png' },
  { t: 'l', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/l.png' },
  { t: 'leftArrow', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/leftArrow.png' },
  { t: 'leftBracket', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/leftBracket.png' },
  { t: 'leftButton', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/leftButton.png' },
  { t: 'm', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/m.png' },
  { t: 'middleButton', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/middleButton.png' },
  { t: 'minus', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/minus.png' },
  { t: 'mouse', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/mouse.png' },
  { t: 'n', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/n.png' },
  { t: 'numLock', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/numLock.png' },
  { t: 'numpadMultiply', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/numpadMultiply.png' },
  { t: 'o', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/o.png' },
  { t: 'p', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/p.png' },
  { t: 'pageDown', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/pageDown.png' },
  { t: 'pageUp', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/pageUp.png' },
  { t: 'printScreen', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/printScreen.png' },
  { t: 'q', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/q.png' },
  { t: 'quote', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/quote.png' },
  { t: 'r', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/r.png' },
  { t: 'rightArrow', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/rightArrow.png' },
  { t: 'rightBracket', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/rightBracket.png' },
  { t: 'rightButton', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/rightButton.png' },
  { t: 's', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/s.png' },
  { t: 'scroll', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/scroll.png' },
  { t: 'scrollDown', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/scrollDown.png' },
  { t: 'scrollHorizontal', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/scrollHorizontal.png' },
  { t: 'scrollUp', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/scrollUp.png' },
  { t: 'scrollVertical', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/scrollVertical.png' },
  { t: 'semicolon', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/semicolon.png' },
  { t: 'shift', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/shift.png' },
  { t: 'slash', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/slash.png' },
  { t: 'space', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/space.png' },
  { t: 't', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/t.png' },
  { t: 'tab', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/tab.png' },
  { t: 'u', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/u.png' },
  { t: 'upArrow', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/upArrow.png' },
  { t: 'v', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/v.png' },
  { t: 'w', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/w.png' },
  { t: 'wasd', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/wasd.png' },
  { t: 'x', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/x.png' },
  { t: 'y', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/y.png' },
  { t: 'z', c: 'Keyboard & Mouse', u: 'res/icons.html', k: 'icono kbm teclado', y: 'ícono', img: 'Assets/_/Library/Input/KeyboardMouse/z.png' },
  { t: 'buttonEast', c: 'Xbox', u: 'res/icons.html', k: 'icono gamepad xbox', y: 'ícono', img: 'Assets/_/Library/Input/Xbox/buttonEast.png' },
  { t: 'buttonEastWhite', c: 'Xbox', u: 'res/icons.html', k: 'icono gamepad xbox', y: 'ícono', img: 'Assets/_/Library/Input/Xbox/buttonEastWhite.png' },
  { t: 'buttonNorth', c: 'Xbox', u: 'res/icons.html', k: 'icono gamepad xbox', y: 'ícono', img: 'Assets/_/Library/Input/Xbox/buttonNorth.png' },
  { t: 'buttonNorthWhite', c: 'Xbox', u: 'res/icons.html', k: 'icono gamepad xbox', y: 'ícono', img: 'Assets/_/Library/Input/Xbox/buttonNorthWhite.png' },
  { t: 'buttonSouth', c: 'Xbox', u: 'res/icons.html', k: 'icono gamepad xbox', y: 'ícono', img: 'Assets/_/Library/Input/Xbox/buttonSouth.png' },
  { t: 'buttonSouthWhite', c: 'Xbox', u: 'res/icons.html', k: 'icono gamepad xbox', y: 'ícono', img: 'Assets/_/Library/Input/Xbox/buttonSouthWhite.png' },
  { t: 'buttonWest', c: 'Xbox', u: 'res/icons.html', k: 'icono gamepad xbox', y: 'ícono', img: 'Assets/_/Library/Input/Xbox/buttonWest.png' },
  { t: 'buttonWestSimple', c: 'Xbox', u: 'res/icons.html', k: 'icono gamepad xbox', y: 'ícono', img: 'Assets/_/Library/Input/Xbox/buttonWestSimple.png' },
  { t: 'buttonWestWhite', c: 'Xbox', u: 'res/icons.html', k: 'icono gamepad xbox', y: 'ícono', img: 'Assets/_/Library/Input/Xbox/buttonWestWhite.png' },
  { t: 'dpad', c: 'Xbox', u: 'res/icons.html', k: 'icono gamepad xbox', y: 'ícono', img: 'Assets/_/Library/Input/Xbox/dpad.png' },
  { t: 'dpadDown', c: 'Xbox', u: 'res/icons.html', k: 'icono gamepad xbox', y: 'ícono', img: 'Assets/_/Library/Input/Xbox/dpadDown.png' },
  { t: 'dpadLeft', c: 'Xbox', u: 'res/icons.html', k: 'icono gamepad xbox', y: 'ícono', img: 'Assets/_/Library/Input/Xbox/dpadLeft.png' },
  { t: 'dpadRight', c: 'Xbox', u: 'res/icons.html', k: 'icono gamepad xbox', y: 'ícono', img: 'Assets/_/Library/Input/Xbox/dpadRight.png' },
  { t: 'dpadUp', c: 'Xbox', u: 'res/icons.html', k: 'icono gamepad xbox', y: 'ícono', img: 'Assets/_/Library/Input/Xbox/dpadUp.png' },
  { t: 'dpadX', c: 'Xbox', u: 'res/icons.html', k: 'icono gamepad xbox', y: 'ícono', img: 'Assets/_/Library/Input/Xbox/dpadX.png' },
  { t: 'dpadY', c: 'Xbox', u: 'res/icons.html', k: 'icono gamepad xbox', y: 'ícono', img: 'Assets/_/Library/Input/Xbox/dpadY.png' },
  { t: 'leftShoulder', c: 'Xbox', u: 'res/icons.html', k: 'icono gamepad xbox', y: 'ícono', img: 'Assets/_/Library/Input/Xbox/leftShoulder.png' },
  { t: 'leftStick', c: 'Xbox', u: 'res/icons.html', k: 'icono gamepad xbox', y: 'ícono', img: 'Assets/_/Library/Input/Xbox/leftStick.png' },
  { t: 'leftStickDown', c: 'Xbox', u: 'res/icons.html', k: 'icono gamepad xbox', y: 'ícono', img: 'Assets/_/Library/Input/Xbox/leftStickDown.png' },
  { t: 'leftStickLeft', c: 'Xbox', u: 'res/icons.html', k: 'icono gamepad xbox', y: 'ícono', img: 'Assets/_/Library/Input/Xbox/leftStickLeft.png' },
  { t: 'leftStickPress', c: 'Xbox', u: 'res/icons.html', k: 'icono gamepad xbox', y: 'ícono', img: 'Assets/_/Library/Input/Xbox/leftStickPress.png' },
  { t: 'leftStickPressAlt', c: 'Xbox', u: 'res/icons.html', k: 'icono gamepad xbox', y: 'ícono', img: 'Assets/_/Library/Input/Xbox/leftStickPressAlt.png' },
  { t: 'leftStickRight', c: 'Xbox', u: 'res/icons.html', k: 'icono gamepad xbox', y: 'ícono', img: 'Assets/_/Library/Input/Xbox/leftStickRight.png' },
  { t: 'leftStickSimple', c: 'Xbox', u: 'res/icons.html', k: 'icono gamepad xbox', y: 'ícono', img: 'Assets/_/Library/Input/Xbox/leftStickSimple.png' },
  { t: 'leftStickUp', c: 'Xbox', u: 'res/icons.html', k: 'icono gamepad xbox', y: 'ícono', img: 'Assets/_/Library/Input/Xbox/leftStickUp.png' },
  { t: 'leftStickX', c: 'Xbox', u: 'res/icons.html', k: 'icono gamepad xbox', y: 'ícono', img: 'Assets/_/Library/Input/Xbox/leftStickX.png' },
  { t: 'leftStickY', c: 'Xbox', u: 'res/icons.html', k: 'icono gamepad xbox', y: 'ícono', img: 'Assets/_/Library/Input/Xbox/leftStickY.png' },
  { t: 'leftTrigger', c: 'Xbox', u: 'res/icons.html', k: 'icono gamepad xbox', y: 'ícono', img: 'Assets/_/Library/Input/Xbox/leftTrigger.png' },
  { t: 'rightShoulder', c: 'Xbox', u: 'res/icons.html', k: 'icono gamepad xbox', y: 'ícono', img: 'Assets/_/Library/Input/Xbox/rightShoulder.png' },
  { t: 'rightStick', c: 'Xbox', u: 'res/icons.html', k: 'icono gamepad xbox', y: 'ícono', img: 'Assets/_/Library/Input/Xbox/rightStick.png' },
  { t: 'rightStickDown', c: 'Xbox', u: 'res/icons.html', k: 'icono gamepad xbox', y: 'ícono', img: 'Assets/_/Library/Input/Xbox/rightStickDown.png' },
  { t: 'rightStickLeft', c: 'Xbox', u: 'res/icons.html', k: 'icono gamepad xbox', y: 'ícono', img: 'Assets/_/Library/Input/Xbox/rightStickLeft.png' },
  { t: 'rightStickPress', c: 'Xbox', u: 'res/icons.html', k: 'icono gamepad xbox', y: 'ícono', img: 'Assets/_/Library/Input/Xbox/rightStickPress.png' },
  { t: 'rightStickPressAlt', c: 'Xbox', u: 'res/icons.html', k: 'icono gamepad xbox', y: 'ícono', img: 'Assets/_/Library/Input/Xbox/rightStickPressAlt.png' },
  { t: 'rightStickRight', c: 'Xbox', u: 'res/icons.html', k: 'icono gamepad xbox', y: 'ícono', img: 'Assets/_/Library/Input/Xbox/rightStickRight.png' },
  { t: 'rightStickSimple', c: 'Xbox', u: 'res/icons.html', k: 'icono gamepad xbox', y: 'ícono', img: 'Assets/_/Library/Input/Xbox/rightStickSimple.png' },
  { t: 'rightStickUp', c: 'Xbox', u: 'res/icons.html', k: 'icono gamepad xbox', y: 'ícono', img: 'Assets/_/Library/Input/Xbox/rightStickUp.png' },
  { t: 'rightStickX', c: 'Xbox', u: 'res/icons.html', k: 'icono gamepad xbox', y: 'ícono', img: 'Assets/_/Library/Input/Xbox/rightStickX.png' },
  { t: 'rightStickY', c: 'Xbox', u: 'res/icons.html', k: 'icono gamepad xbox', y: 'ícono', img: 'Assets/_/Library/Input/Xbox/rightStickY.png' },
  { t: 'rightTrigger', c: 'Xbox', u: 'res/icons.html', k: 'icono gamepad xbox', y: 'ícono', img: 'Assets/_/Library/Input/Xbox/rightTrigger.png' },
  { t: 'selectButton', c: 'Xbox', u: 'res/icons.html', k: 'icono gamepad xbox', y: 'ícono', img: 'Assets/_/Library/Input/Xbox/selectButton.png' },
  { t: 'selectButtonAlt', c: 'Xbox', u: 'res/icons.html', k: 'icono gamepad xbox', y: 'ícono', img: 'Assets/_/Library/Input/Xbox/selectButtonAlt.png' },
  { t: 'buttonEast', c: 'PlayStation 4', u: 'res/icons.html', k: 'icono gamepad ps4 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS4/buttonEast.png' },
  { t: 'buttonEastSimple', c: 'PlayStation 4', u: 'res/icons.html', k: 'icono gamepad ps4 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS4/buttonEastSimple.png' },
  { t: 'buttonNorth', c: 'PlayStation 4', u: 'res/icons.html', k: 'icono gamepad ps4 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS4/buttonNorth.png' },
  { t: 'buttonNorthSimple', c: 'PlayStation 4', u: 'res/icons.html', k: 'icono gamepad ps4 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS4/buttonNorthSimple.png' },
  { t: 'buttonSouth', c: 'PlayStation 4', u: 'res/icons.html', k: 'icono gamepad ps4 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS4/buttonSouth.png' },
  { t: 'buttonSouthSimple', c: 'PlayStation 4', u: 'res/icons.html', k: 'icono gamepad ps4 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS4/buttonSouthSimple.png' },
  { t: 'buttonWest', c: 'PlayStation 4', u: 'res/icons.html', k: 'icono gamepad ps4 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS4/buttonWest.png' },
  { t: 'buttonWestSimple', c: 'PlayStation 4', u: 'res/icons.html', k: 'icono gamepad ps4 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS4/buttonWestSimple.png' },
  { t: 'dpad', c: 'PlayStation 4', u: 'res/icons.html', k: 'icono gamepad ps4 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS4/dpad.png' },
  { t: 'dpadDown', c: 'PlayStation 4', u: 'res/icons.html', k: 'icono gamepad ps4 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS4/dpadDown.png' },
  { t: 'dpadLeft', c: 'PlayStation 4', u: 'res/icons.html', k: 'icono gamepad ps4 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS4/dpadLeft.png' },
  { t: 'dpadRight', c: 'PlayStation 4', u: 'res/icons.html', k: 'icono gamepad ps4 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS4/dpadRight.png' },
  { t: 'dpadUp', c: 'PlayStation 4', u: 'res/icons.html', k: 'icono gamepad ps4 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS4/dpadUp.png' },
  { t: 'dpadX', c: 'PlayStation 4', u: 'res/icons.html', k: 'icono gamepad ps4 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS4/dpadX.png' },
  { t: 'dpadY', c: 'PlayStation 4', u: 'res/icons.html', k: 'icono gamepad ps4 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS4/dpadY.png' },
  { t: 'leftShoulder', c: 'PlayStation 4', u: 'res/icons.html', k: 'icono gamepad ps4 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS4/leftShoulder.png' },
  { t: 'leftStick', c: 'PlayStation 4', u: 'res/icons.html', k: 'icono gamepad ps4 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS4/leftStick.png' },
  { t: 'leftStickDown', c: 'PlayStation 4', u: 'res/icons.html', k: 'icono gamepad ps4 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS4/leftStickDown.png' },
  { t: 'leftStickLeft', c: 'PlayStation 4', u: 'res/icons.html', k: 'icono gamepad ps4 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS4/leftStickLeft.png' },
  { t: 'leftStickPress', c: 'PlayStation 4', u: 'res/icons.html', k: 'icono gamepad ps4 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS4/leftStickPress.png' },
  { t: 'leftStickPressAlt', c: 'PlayStation 4', u: 'res/icons.html', k: 'icono gamepad ps4 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS4/leftStickPressAlt.png' },
  { t: 'leftStickRight', c: 'PlayStation 4', u: 'res/icons.html', k: 'icono gamepad ps4 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS4/leftStickRight.png' },
  { t: 'leftStickSimple', c: 'PlayStation 4', u: 'res/icons.html', k: 'icono gamepad ps4 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS4/leftStickSimple.png' },
  { t: 'leftStickUp', c: 'PlayStation 4', u: 'res/icons.html', k: 'icono gamepad ps4 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS4/leftStickUp.png' },
  { t: 'leftStickX', c: 'PlayStation 4', u: 'res/icons.html', k: 'icono gamepad ps4 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS4/leftStickX.png' },
  { t: 'leftStickY', c: 'PlayStation 4', u: 'res/icons.html', k: 'icono gamepad ps4 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS4/leftStickY.png' },
  { t: 'leftTrigger', c: 'PlayStation 4', u: 'res/icons.html', k: 'icono gamepad ps4 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS4/leftTrigger.png' },
  { t: 'rightShoulder', c: 'PlayStation 4', u: 'res/icons.html', k: 'icono gamepad ps4 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS4/rightShoulder.png' },
  { t: 'rightStick', c: 'PlayStation 4', u: 'res/icons.html', k: 'icono gamepad ps4 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS4/rightStick.png' },
  { t: 'rightStickDown', c: 'PlayStation 4', u: 'res/icons.html', k: 'icono gamepad ps4 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS4/rightStickDown.png' },
  { t: 'rightStickLeft', c: 'PlayStation 4', u: 'res/icons.html', k: 'icono gamepad ps4 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS4/rightStickLeft.png' },
  { t: 'rightStickPress', c: 'PlayStation 4', u: 'res/icons.html', k: 'icono gamepad ps4 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS4/rightStickPress.png' },
  { t: 'rightStickPressAlt', c: 'PlayStation 4', u: 'res/icons.html', k: 'icono gamepad ps4 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS4/rightStickPressAlt.png' },
  { t: 'rightStickRight', c: 'PlayStation 4', u: 'res/icons.html', k: 'icono gamepad ps4 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS4/rightStickRight.png' },
  { t: 'rightStickSimple', c: 'PlayStation 4', u: 'res/icons.html', k: 'icono gamepad ps4 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS4/rightStickSimple.png' },
  { t: 'rightStickUp', c: 'PlayStation 4', u: 'res/icons.html', k: 'icono gamepad ps4 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS4/rightStickUp.png' },
  { t: 'rightStickX', c: 'PlayStation 4', u: 'res/icons.html', k: 'icono gamepad ps4 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS4/rightStickX.png' },
  { t: 'rightStickY', c: 'PlayStation 4', u: 'res/icons.html', k: 'icono gamepad ps4 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS4/rightStickY.png' },
  { t: 'rightTrigger', c: 'PlayStation 4', u: 'res/icons.html', k: 'icono gamepad ps4 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS4/rightTrigger.png' },
  { t: 'select', c: 'PlayStation 4', u: 'res/icons.html', k: 'icono gamepad ps4 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS4/select.png' },
  { t: 'start', c: 'PlayStation 4', u: 'res/icons.html', k: 'icono gamepad ps4 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS4/start.png' },
  { t: 'touchpadButton', c: 'PlayStation 4', u: 'res/icons.html', k: 'icono gamepad ps4 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS4/touchpadButton.png' },
  { t: 'buttonEast', c: 'PlayStation 5', u: 'res/icons.html', k: 'icono gamepad ps5 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS5/buttonEast.png' },
  { t: 'buttonEastSimple', c: 'PlayStation 5', u: 'res/icons.html', k: 'icono gamepad ps5 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS5/buttonEastSimple.png' },
  { t: 'buttonNorth', c: 'PlayStation 5', u: 'res/icons.html', k: 'icono gamepad ps5 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS5/buttonNorth.png' },
  { t: 'buttonNorthSimple', c: 'PlayStation 5', u: 'res/icons.html', k: 'icono gamepad ps5 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS5/buttonNorthSimple.png' },
  { t: 'buttonSouth', c: 'PlayStation 5', u: 'res/icons.html', k: 'icono gamepad ps5 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS5/buttonSouth.png' },
  { t: 'buttonSouthSimple', c: 'PlayStation 5', u: 'res/icons.html', k: 'icono gamepad ps5 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS5/buttonSouthSimple.png' },
  { t: 'buttonWest', c: 'PlayStation 5', u: 'res/icons.html', k: 'icono gamepad ps5 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS5/buttonWest.png' },
  { t: 'buttonWestSimple', c: 'PlayStation 5', u: 'res/icons.html', k: 'icono gamepad ps5 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS5/buttonWestSimple.png' },
  { t: 'dpad', c: 'PlayStation 5', u: 'res/icons.html', k: 'icono gamepad ps5 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS5/dpad.png' },
  { t: 'dpadDown', c: 'PlayStation 5', u: 'res/icons.html', k: 'icono gamepad ps5 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS5/dpadDown.png' },
  { t: 'dpadLeft', c: 'PlayStation 5', u: 'res/icons.html', k: 'icono gamepad ps5 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS5/dpadLeft.png' },
  { t: 'dpadRight', c: 'PlayStation 5', u: 'res/icons.html', k: 'icono gamepad ps5 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS5/dpadRight.png' },
  { t: 'dpadUp', c: 'PlayStation 5', u: 'res/icons.html', k: 'icono gamepad ps5 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS5/dpadUp.png' },
  { t: 'dpadX', c: 'PlayStation 5', u: 'res/icons.html', k: 'icono gamepad ps5 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS5/dpadX.png' },
  { t: 'dpadY', c: 'PlayStation 5', u: 'res/icons.html', k: 'icono gamepad ps5 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS5/dpadY.png' },
  { t: 'leftShoulder', c: 'PlayStation 5', u: 'res/icons.html', k: 'icono gamepad ps5 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS5/leftShoulder.png' },
  { t: 'leftStick', c: 'PlayStation 5', u: 'res/icons.html', k: 'icono gamepad ps5 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS5/leftStick.png' },
  { t: 'leftStickDown', c: 'PlayStation 5', u: 'res/icons.html', k: 'icono gamepad ps5 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS5/leftStickDown.png' },
  { t: 'leftStickLeft', c: 'PlayStation 5', u: 'res/icons.html', k: 'icono gamepad ps5 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS5/leftStickLeft.png' },
  { t: 'leftStickPressAlt', c: 'PlayStation 5', u: 'res/icons.html', k: 'icono gamepad ps5 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS5/leftStickPressAlt.png' },
  { t: 'leftStickRight', c: 'PlayStation 5', u: 'res/icons.html', k: 'icono gamepad ps5 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS5/leftStickRight.png' },
  { t: 'leftStickSimple', c: 'PlayStation 5', u: 'res/icons.html', k: 'icono gamepad ps5 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS5/leftStickSimple.png' },
  { t: 'leftStickUp', c: 'PlayStation 5', u: 'res/icons.html', k: 'icono gamepad ps5 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS5/leftStickUp.png' },
  { t: 'leftStickX', c: 'PlayStation 5', u: 'res/icons.html', k: 'icono gamepad ps5 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS5/leftStickX.png' },
  { t: 'leftStickY', c: 'PlayStation 5', u: 'res/icons.html', k: 'icono gamepad ps5 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS5/leftStickY.png' },
  { t: 'leftTrigger', c: 'PlayStation 5', u: 'res/icons.html', k: 'icono gamepad ps5 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS5/leftTrigger.png' },
  { t: 'rightShoulder', c: 'PlayStation 5', u: 'res/icons.html', k: 'icono gamepad ps5 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS5/rightShoulder.png' },
  { t: 'rightStick', c: 'PlayStation 5', u: 'res/icons.html', k: 'icono gamepad ps5 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS5/rightStick.png' },
  { t: 'rightStickDown', c: 'PlayStation 5', u: 'res/icons.html', k: 'icono gamepad ps5 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS5/rightStickDown.png' },
  { t: 'rightStickLeft', c: 'PlayStation 5', u: 'res/icons.html', k: 'icono gamepad ps5 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS5/rightStickLeft.png' },
  { t: 'rightStickPressAlt', c: 'PlayStation 5', u: 'res/icons.html', k: 'icono gamepad ps5 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS5/rightStickPressAlt.png' },
  { t: 'rightStickRight', c: 'PlayStation 5', u: 'res/icons.html', k: 'icono gamepad ps5 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS5/rightStickRight.png' },
  { t: 'rightStickSimple', c: 'PlayStation 5', u: 'res/icons.html', k: 'icono gamepad ps5 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS5/rightStickSimple.png' },
  { t: 'rightStickUp', c: 'PlayStation 5', u: 'res/icons.html', k: 'icono gamepad ps5 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS5/rightStickUp.png' },
  { t: 'rightStickX', c: 'PlayStation 5', u: 'res/icons.html', k: 'icono gamepad ps5 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS5/rightStickX.png' },
  { t: 'rightStickY', c: 'PlayStation 5', u: 'res/icons.html', k: 'icono gamepad ps5 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS5/rightStickY.png' },
  { t: 'rightTrigger', c: 'PlayStation 5', u: 'res/icons.html', k: 'icono gamepad ps5 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS5/rightTrigger.png' },
  { t: 'select', c: 'PlayStation 5', u: 'res/icons.html', k: 'icono gamepad ps5 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS5/select.png' },
  { t: 'start', c: 'PlayStation 5', u: 'res/icons.html', k: 'icono gamepad ps5 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS5/start.png' },
  { t: 'touchpadButton', c: 'PlayStation 5', u: 'res/icons.html', k: 'icono gamepad ps5 playstation', y: 'ícono', img: 'Assets/_/Library/Input/PS5/touchpadButton.png' },
  { t: 'buttonEast', c: 'Switch', u: 'res/icons.html', k: 'icono gamepad switch nintendo', y: 'ícono', img: 'Assets/_/Library/Input/Switch/buttonEast.png' },
  { t: 'buttonNorth', c: 'Switch', u: 'res/icons.html', k: 'icono gamepad switch nintendo', y: 'ícono', img: 'Assets/_/Library/Input/Switch/buttonNorth.png' },
  { t: 'buttonSouth', c: 'Switch', u: 'res/icons.html', k: 'icono gamepad switch nintendo', y: 'ícono', img: 'Assets/_/Library/Input/Switch/buttonSouth.png' },
  { t: 'buttonWest', c: 'Switch', u: 'res/icons.html', k: 'icono gamepad switch nintendo', y: 'ícono', img: 'Assets/_/Library/Input/Switch/buttonWest.png' },
  { t: 'dpad', c: 'Switch', u: 'res/icons.html', k: 'icono gamepad switch nintendo', y: 'ícono', img: 'Assets/_/Library/Input/Switch/dpad.png' },
  { t: 'dpadDown', c: 'Switch', u: 'res/icons.html', k: 'icono gamepad switch nintendo', y: 'ícono', img: 'Assets/_/Library/Input/Switch/dpadDown.png' },
  { t: 'dpadLeft', c: 'Switch', u: 'res/icons.html', k: 'icono gamepad switch nintendo', y: 'ícono', img: 'Assets/_/Library/Input/Switch/dpadLeft.png' },
  { t: 'dpadRight', c: 'Switch', u: 'res/icons.html', k: 'icono gamepad switch nintendo', y: 'ícono', img: 'Assets/_/Library/Input/Switch/dpadRight.png' },
  { t: 'dpadUp', c: 'Switch', u: 'res/icons.html', k: 'icono gamepad switch nintendo', y: 'ícono', img: 'Assets/_/Library/Input/Switch/dpadUp.png' },
  { t: 'dpadX', c: 'Switch', u: 'res/icons.html', k: 'icono gamepad switch nintendo', y: 'ícono', img: 'Assets/_/Library/Input/Switch/dpadX.png' },
  { t: 'dpadY', c: 'Switch', u: 'res/icons.html', k: 'icono gamepad switch nintendo', y: 'ícono', img: 'Assets/_/Library/Input/Switch/dpadY.png' },
  { t: 'leftShoulder', c: 'Switch', u: 'res/icons.html', k: 'icono gamepad switch nintendo', y: 'ícono', img: 'Assets/_/Library/Input/Switch/leftShoulder.png' },
  { t: 'leftStick', c: 'Switch', u: 'res/icons.html', k: 'icono gamepad switch nintendo', y: 'ícono', img: 'Assets/_/Library/Input/Switch/leftStick.png' },
  { t: 'leftStickDown', c: 'Switch', u: 'res/icons.html', k: 'icono gamepad switch nintendo', y: 'ícono', img: 'Assets/_/Library/Input/Switch/leftStickDown.png' },
  { t: 'leftStickLeft', c: 'Switch', u: 'res/icons.html', k: 'icono gamepad switch nintendo', y: 'ícono', img: 'Assets/_/Library/Input/Switch/leftStickLeft.png' },
  { t: 'leftStickRight', c: 'Switch', u: 'res/icons.html', k: 'icono gamepad switch nintendo', y: 'ícono', img: 'Assets/_/Library/Input/Switch/leftStickRight.png' },
  { t: 'leftStickSimple', c: 'Switch', u: 'res/icons.html', k: 'icono gamepad switch nintendo', y: 'ícono', img: 'Assets/_/Library/Input/Switch/leftStickSimple.png' },
  { t: 'leftStickUp', c: 'Switch', u: 'res/icons.html', k: 'icono gamepad switch nintendo', y: 'ícono', img: 'Assets/_/Library/Input/Switch/leftStickUp.png' },
  { t: 'leftStickX', c: 'Switch', u: 'res/icons.html', k: 'icono gamepad switch nintendo', y: 'ícono', img: 'Assets/_/Library/Input/Switch/leftStickX.png' },
  { t: 'leftStickY', c: 'Switch', u: 'res/icons.html', k: 'icono gamepad switch nintendo', y: 'ícono', img: 'Assets/_/Library/Input/Switch/leftStickY.png' },
  { t: 'leftTrigger', c: 'Switch', u: 'res/icons.html', k: 'icono gamepad switch nintendo', y: 'ícono', img: 'Assets/_/Library/Input/Switch/leftTrigger.png' },
  { t: 'rightShoulder', c: 'Switch', u: 'res/icons.html', k: 'icono gamepad switch nintendo', y: 'ícono', img: 'Assets/_/Library/Input/Switch/rightShoulder.png' },
  { t: 'rightStick', c: 'Switch', u: 'res/icons.html', k: 'icono gamepad switch nintendo', y: 'ícono', img: 'Assets/_/Library/Input/Switch/rightStick.png' },
  { t: 'rightStickDown', c: 'Switch', u: 'res/icons.html', k: 'icono gamepad switch nintendo', y: 'ícono', img: 'Assets/_/Library/Input/Switch/rightStickDown.png' },
  { t: 'rightStickLeft', c: 'Switch', u: 'res/icons.html', k: 'icono gamepad switch nintendo', y: 'ícono', img: 'Assets/_/Library/Input/Switch/rightStickLeft.png' },
  { t: 'rightStickRight', c: 'Switch', u: 'res/icons.html', k: 'icono gamepad switch nintendo', y: 'ícono', img: 'Assets/_/Library/Input/Switch/rightStickRight.png' },
  { t: 'rightStickSimple', c: 'Switch', u: 'res/icons.html', k: 'icono gamepad switch nintendo', y: 'ícono', img: 'Assets/_/Library/Input/Switch/rightStickSimple.png' },
  { t: 'rightStickUp', c: 'Switch', u: 'res/icons.html', k: 'icono gamepad switch nintendo', y: 'ícono', img: 'Assets/_/Library/Input/Switch/rightStickUp.png' },
  { t: 'rightStickX', c: 'Switch', u: 'res/icons.html', k: 'icono gamepad switch nintendo', y: 'ícono', img: 'Assets/_/Library/Input/Switch/rightStickX.png' },
  { t: 'rightStickY', c: 'Switch', u: 'res/icons.html', k: 'icono gamepad switch nintendo', y: 'ícono', img: 'Assets/_/Library/Input/Switch/rightStickY.png' },
  { t: 'rightTrigger', c: 'Switch', u: 'res/icons.html', k: 'icono gamepad switch nintendo', y: 'ícono', img: 'Assets/_/Library/Input/Switch/rightTrigger.png' },
  { t: 'select', c: 'Switch', u: 'res/icons.html', k: 'icono gamepad switch nintendo', y: 'ícono', img: 'Assets/_/Library/Input/Switch/select.png' },
  { t: 'start', c: 'Switch', u: 'res/icons.html', k: 'icono gamepad switch nintendo', y: 'ícono', img: 'Assets/_/Library/Input/Switch/start.png' },
];

(function () {
  let box, input, list, sel = 0, results = [];

  function build() {
    box = document.createElement('div');
    box.id = 'qsearch';
    box.innerHTML = `
      <div class="qs-box">
        <input class="qs-in" type="search" placeholder="Buscar en toda la docu…" spellcheck="false">
        <div class="qs-list"></div>
        <div class="qs-foot"><span>↑↓ navegar</span><span>↵ abrir</span><span>esc cerrar</span><span class="sp"></span><span>⚛ quark</span></div>
      </div>`;
    document.body.appendChild(box);
    input = box.querySelector('.qs-in');
    list = box.querySelector('.qs-list');
    box.addEventListener('mousedown', e => { if (e.target === box) close(); });
    input.addEventListener('input', () => render(input.value));
    input.addEventListener('keydown', e => {
      if (e.key === 'ArrowDown') { e.preventDefault(); move(1); }
      else if (e.key === 'ArrowUp') { e.preventDefault(); move(-1); }
      else if (e.key === 'Enter' && results[sel]) go(results[sel]);
    });
  }

  function score(e, q) {
    const t = e.t.toLowerCase(), k = (e.k + ' ' + e.c).toLowerCase();
    if (t === q) return 0;
    if (t.startsWith(q)) return 1;
    if (t.includes(q)) return 2;
    if (k.includes(q)) return 3;
    return -1;
  }

  function render(q) {
    q = q.trim().toLowerCase();
    results = !q
      ? QINDEX.filter(e => e.y === 'página')
      : QINDEX.map(e => [score(e, q), e]).filter(([s]) => s >= 0)
              .sort((a, b) => a[0] - b[0]).slice(0, 24).map(([, e]) => e);
    sel = 0;
    list.innerHTML = results.length ? '' : '<div class="qs-empty">Sin resultados. Probá con otra key.</div>';
    results.forEach((e, i) => {
      const b = document.createElement('button');
      b.className = 'qs-item' + (i === sel ? ' sel' : '');
      b.innerHTML = (e.img ? `<img loading="lazy" src="${QROOT}../${e.img}" alt="">` : '') +
        `<span class="t"><b>${e.t}</b><span class="ctx">${e.c}</span></span><span class="type">${e.y}</span>`;
      b.addEventListener('click', () => go(e));
      b.addEventListener('mousemove', () => { sel = i; paint(); });
      list.appendChild(b);
    });
  }

  function paint() {
    [...list.children].forEach((el, i) => el.classList && el.classList.toggle('sel', i === sel));
  }

  function move(d) {
    if (!results.length) return;
    sel = (sel + d + results.length) % results.length;
    paint();
    list.children[sel].scrollIntoView({ block: 'nearest' });
  }

  function go(e) { location.href = QROOT + e.u; }

  window.openSearch = function () {
    if (!box) build();
    box.classList.add('open');
    input.value = '';
    render('');
    input.focus();
  };

  function close() { if (box) box.classList.remove('open'); }

  document.addEventListener('keydown', e => {
    if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k') { e.preventDefault(); openSearch(); }
    else if (e.key === '/' && !/INPUT|TEXTAREA/.test(document.activeElement.tagName)) { e.preventDefault(); openSearch(); }
    else if (e.key === 'Escape') close();
  });
})();
