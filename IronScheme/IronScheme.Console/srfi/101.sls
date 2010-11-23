(library (srfi :101)
  (export quote
          pair? 
          cons
          car 
          cdr
          caar 
          cadr
          cddr
          cdar
          caaar
          caadr
          caddr
          cadar
          cdaar
          cdadr
          cdddr
          cddar
          caaaar
          caaadr
          caaddr
          caadar
          cadaar
          cadadr
          cadddr
          caddar
          cdaaar
          cdaadr
          cdaddr
          cdadar
          cddaar
          cddadr
          cddddr
          cdddar
          null?
          list?
          list
          make-list
          length
          append
          reverse
          list-tail
          list-ref
          list-set
          list-ref/update
          map
          for-each
          random-access-list->linear-access-list
          linear-access-list->random-access-list)
  (import (srfi :101 random-access-lists)))