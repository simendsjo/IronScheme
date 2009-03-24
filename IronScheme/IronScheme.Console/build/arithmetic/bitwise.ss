(library (ironscheme arithmetic bitwise)
  (export
    bitwise-not
    
    bitwise-and
    bitwise-ior
    bitwise-xor
    
    bitwise-if
    
    bitwise-bit-count
    bitwise-length
    bitwise-bit-field
    
    bitwise-first-bit-set
    bitwise-bit-set?
    
    bitwise-copy-bit
    bitwise-copy-bit-field
    
    bitwise-arithmetic-shift
    bitwise-arithmetic-shift-left
    bitwise-arithmetic-shift-right
    
    bitwise-rotate-bit-field
    bitwise-reverse-bit-field)
    
  (import 
    (ironscheme clr)
    (except (rnrs) 
      bitwise-if 
      bitwise-copy-bit 
      bitwise-bit-field 
      bitwise-copy-bit-field
      bitwise-arithmetic-shift-left 
      bitwise-arithmetic-shift-right
      bitwise-rotate-bit-field
      bitwise-not
      bitwise-and
      bitwise-ior
      bitwise-xor
      bitwise-bit-count
      bitwise-length
      bitwise-first-bit-set
      bitwise-bit-set?
      ))
      
  (define (bignum? obj)
    (clr-is Microsoft.Scripting.Math.BigInteger obj))      
    
  (define (->bignum ei)
    (cond
      [(bignum? ei) ei]
      [(fixnum? ei) 
        (clr-static-call Microsoft.Scripting.Math.BigInteger "Create(System.Int32)" ei)]
      [else
        (assertion-violation #f "not a exact integer" ei)]))
        
  (define (bitwise-not ei)
    (exact (clr-static-call Microsoft.Scripting.Math.BigInteger 
                            op_OnesComplement  
                            (->bignum ei))))
                            
  (define bitwise-and
    (case-lambda
      [()  -1]
      [(ei)
        (->bignum ei)]
      [(ei1 ei2)
        (exact (clr-static-call Microsoft.Scripting.Math.BigInteger 
                                op_BitwiseAnd  
                                (->bignum ei1)
                                (->bignum ei2)))]
      [(ei1 ei2 . rest)
        (fold-left bitwise-and (->bignum ei1) (cons ei2 rest))]))
        
  (define bitwise-ior
    (case-lambda
      [()  0]
      [(ei)
        (->bignum ei)]
      [(ei1 ei2)
        (exact (clr-static-call Microsoft.Scripting.Math.BigInteger 
                                op_BitwiseOr  
                                (->bignum ei1)
                                (->bignum ei2)))]
      [(ei1 ei2 . rest)
        (fold-left bitwise-ior (->bignum ei1) (cons ei2 rest))]))

  (define bitwise-xor
    (case-lambda
      [()  0]
      [(ei)
        (->bignum ei)]
      [(ei1 ei2)
        (exact (clr-static-call Microsoft.Scripting.Math.BigInteger 
                                op_ExclusiveOr  
                                (->bignum ei1)
                                (->bignum ei2)))]
      [(ei1 ei2 . rest)
        (fold-left bitwise-xor (->bignum ei1) (cons ei2 rest))]))
        
  (define (bitwise-bit-count ei)
    (if (positive? ei)
        (let f ((c 0)(ei (->bignum ei)))
          (if (positive? ei)
              (f (+ c (bitwise-and ei 1))
                 (bitwise-arithmetic-shift-right ei 1))
              c))
        (bitwise-not (bitwise-bit-count (bitwise-not ei)))))    
        
  (define (bitwise-length ei)
    (let ((ei (->bignum ei)))
      (if (clr-static-call Microsoft.Scripting.Math.BigInteger 
                           op_LessThan
                           ei
                           (->bignum 0))
          (bitwise-length (bitwise-not ei))
          (clr-prop-get Microsoft.Scripting.Math.BigInteger BitLength ei))))
          
  (define (bitwise-first-bit-set ei)
    (let ((ei (->bignum ei)))
      (if (zero? ei)
          -1
          (let f ((c 0)(ei ei))
            (if (= 1 (bitwise-and ei 1))
                c
                (f (+ c 1) (bitwise-arithmetic-shift-right ei 1)))))))

  (define (bitwise-bit-set? ei k)
    (let ((ei (->bignum ei))
          (k (->bignum k)))
      (when (negative? k)
        (assertion-violation 'bitwise-bit-set? "cannot be negative" k))
      (cond
        [(= -1 ei) #t]
        [else
          (= 1 (bitwise-and 1 (bitwise-arithmetic-shift-right ei k)))])))
  
  (define (bitwise-if ei1 ei2 ei3)
    (bitwise-ior (bitwise-and ei1 ei2)
      (bitwise-and (bitwise-not ei1) ei3)))
      
  (define (bitwise-copy-bit ei1 ei2 ei3)
    (bitwise-if 
      (bitwise-arithmetic-shift-left 1 ei2)
      (bitwise-arithmetic-shift-left ei3 ei2)
      ei1))
        
  (define (bitwise-bit-field ei1 ei2 ei3)
    (bitwise-arithmetic-shift-right
      (bitwise-and ei1 (bitwise-not (bitwise-arithmetic-shift-left -1 ei3)))
      ei2))
  
  (define (bitwise-copy-bit-field to start end from)
    (bitwise-if 
      (bitwise-and 
        (bitwise-arithmetic-shift-left -1 start) 
        (bitwise-not (bitwise-arithmetic-shift-left -1 end)))
      (bitwise-arithmetic-shift-left from start)
      to))
                  
  (define (bitwise-arithmetic-shift-left ei1 ei2)
    (bitwise-arithmetic-shift ei1 ei2))            
    
  (define (bitwise-arithmetic-shift-right ei1 ei2)
    (bitwise-arithmetic-shift ei1 (- ei2)))            
    
  (define (bitwise-rotate-bit-field n start end count)
    (let ((width (- end start)))
      (if (positive? width)
        (let ((count (mod count width))
              (field (bitwise-bit-field n start end)))
           (bitwise-copy-bit-field n start end 
            (bitwise-ior 
              (bitwise-arithmetic-shift-left field count) 
              (bitwise-arithmetic-shift-right field (- width count)))))
        n)))
)